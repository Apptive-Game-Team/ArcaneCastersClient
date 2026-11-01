using System;
using System.Collections.Generic;
using GameScene.Dto;
using NUnit.Framework;

namespace WordOnline.Tests
{
    public class TutorialPlaybackBufferTests
    {
        private static TutorialFrameDto Frame(int number, int tick) => new TutorialFrameDto { frame = number, tick = tick };
        private static TutorialStateDto State(int last, int tick, int revision = 0, params TutorialFrameDto[] frames) =>
            new TutorialStateDto { lastFrame = last, tick = tick, tickRate = 20, revision = revision, frames = frames };
        private static TutorialPlaybackBuffer NewBuffer()
        {
            var buffer = new TutorialPlaybackBuffer();
            Assert.That(buffer.Receive(State(1, 0), 0), Is.True);
            return buffer;
        }

        [Test]
        public void BatchedTicksPlayAtFiftyMillisecondIntervalsInsteadOfOneRenderFrame()
        {
            var buffer = NewBuffer();
            Assert.That(buffer.Receive(State(4, 3, 0, Frame(2, 1), Frame(3, 2), Frame(4, 3)), .15), Is.False);
            Assert.That(buffer.ReceivedFrame, Is.EqualTo(4));
            Assert.That(buffer.PlayedFrame, Is.EqualTo(1));
            var times = new List<double>();
            var ticks = new List<int>();
            for (int i = 0; i <= 25; i++)
            {
                double now = .15 + i * .01;
                buffer.BeginUpdate(now);
                while (buffer.TryTake(now, out var frame)) { times.Add(now); ticks.Add(frame.tick); }
            }
            Assert.That(ticks, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(times[0], Is.EqualTo(.25).Within(.00001));
            Assert.That(times[1] - times[0], Is.EqualTo(.05).Within(.00001));
            Assert.That(times[2] - times[1], Is.EqualTo(.05).Within(.00001));
            Assert.That(buffer.IsSettled(.39), Is.False);
            Assert.That(buffer.IsSettled(.4), Is.True);
        }

        [Test]
        public void NewCastAtAnAlreadyDisplayedTickPlaysOnceEvenAcrossResponses()
        {
            var buffer = NewBuffer();
            buffer.Receive(State(2, 0, 0, Frame(2, 0)), .01);
            buffer.BeginUpdate(.01);
            Assert.That(buffer.TryTake(.01, out var first), Is.True);
            Assert.That(first.frame, Is.EqualTo(2));
            buffer.Receive(State(3, 0, 0, Frame(2, 0), Frame(3, 0)), .02);
            buffer.BeginUpdate(.02);
            Assert.That(buffer.TryTake(.02, out var second), Is.True);
            Assert.That(second.frame, Is.EqualTo(3));
            buffer.Receive(State(3, 0, 0, Frame(2, 0), Frame(3, 0)), .03);
            buffer.BeginUpdate(.03);
            Assert.That(buffer.TryTake(.03, out _), Is.False);
        }

        [Test]
        public void SnapshotRecoveryDiscardsOldFramesAndSetsBothWatermarks()
        {
            var buffer = NewBuffer();
            buffer.Receive(State(3, 2, 0, Frame(2, 1), Frame(3, 2)), .1);
            Assert.That(buffer.Receive(State(7, 6, 0, Frame(6, 5), Frame(7, 6)), .2), Is.True);
            Assert.That(buffer.PlayedFrame, Is.EqualTo(7));
            Assert.That(buffer.ReceivedFrame, Is.EqualTo(7));
            Assert.That(buffer.Count, Is.Zero);
            buffer.BeginUpdate(.21);
            Assert.That(buffer.TryTake(.21, out _), Is.False);
        }

        [Test]
        public void RevisionChangeDropsPendingOldCombatAndStartsFromNewSnapshot()
        {
            var buffer = NewBuffer();
            buffer.Receive(State(3, 2, 0, Frame(2, 1), Frame(3, 2)), .1);
            Assert.That(buffer.Receive(State(4, 0, 1, Frame(4, 0)), .2), Is.True);
            Assert.That(buffer.PlayedFrame, Is.EqualTo(4));
            Assert.That(buffer.Count, Is.Zero);
            buffer.Receive(State(5, 0, 1, Frame(5, 0)), .21);
            buffer.BeginUpdate(.21);
            Assert.That(buffer.TryTake(.21, out var cast), Is.True);
            Assert.That(cast.frame, Is.EqualTo(5));
        }

        [Test]
        public void QueueUnderrunRebuffersInsteadOfJumpingThroughSeveralTicks()
        {
            var buffer = NewBuffer();
            buffer.Receive(State(2, 1, 0, Frame(2, 1)), .05);
            for (int i = 0; i <= 15; i++)
            {
                double now = .05 + i * .01;
                buffer.BeginUpdate(now);
                buffer.TryTake(now, out _);
            }
            Assert.That(buffer.Count, Is.Zero);
            buffer.Receive(State(4, 3, 0, Frame(3, 2), Frame(4, 3)), .3);
            buffer.BeginUpdate(.3);
            Assert.That(buffer.TryTake(.3, out _), Is.False);
            for (int i = 1; i <= 10; i++) buffer.BeginUpdate(.3 + i * .01);
            Assert.That(buffer.TryTake(.4, out var frame), Is.True);
            Assert.That(frame.tick, Is.EqualTo(2));
            Assert.That(buffer.TryTake(.4, out _), Is.False);
        }

        [Test]
        public void LongAppStallRequestsSnapshotWithoutReplayingAttacksInABurst()
        {
            var buffer = NewBuffer();
            buffer.Receive(State(3, 2, 0, Frame(2, 1), Frame(3, 2)), .1);
            buffer.BeginUpdate(.8);
            Assert.That(buffer.NeedsSync, Is.True);
            Assert.That(buffer.TryTake(.8, out _), Is.False);
            Assert.That(buffer.Receive(State(4, 3, 0, Frame(4, 3)), .81), Is.True);
            Assert.That(buffer.PlayedFrame, Is.EqualTo(4));
            Assert.That(buffer.NeedsSync, Is.False);
        }

        [Test]
        public void BacklogAndCaptureCountAreBoundedBySnapshotRecovery()
        {
            var buffer = NewBuffer();
            var backlog = new TutorialFrameDto[11];
            for (int i = 0; i < backlog.Length; i++) backlog[i] = Frame(i + 2, i + 1);
            Assert.That(buffer.Receive(State(12, 11, 0, backlog), .2), Is.True);
            Assert.That(buffer.Count, Is.Zero);
            var casts = new TutorialFrameDto[101];
            for (int i = 0; i < casts.Length; i++) casts[i] = Frame(i + 13, 11);
            Assert.That(buffer.Receive(State(113, 11, 0, casts), .21), Is.True);
            Assert.That(buffer.Count, Is.Zero);
            Assert.That(buffer.PlayedFrame, Is.EqualTo(113));
        }

        [Test]
        public void ExplicitReconnectDoesNotReplayTheHistoryIncludedInItsSnapshotResponse()
        {
            var buffer = NewBuffer();
            buffer.Invalidate();
            Assert.That(buffer.Receive(State(3, 2, 0, Frame(1, 0), Frame(2, 1), Frame(3, 2)), .2), Is.True);
            buffer.BeginUpdate(.21);
            Assert.That(buffer.TryTake(.21, out _), Is.False);
        }

        [Test]
        public void RenderStallNeverDrainsDifferentSimulationTicksInOneUpdate()
        {
            var buffer = NewBuffer();
            buffer.Receive(State(4, 3, 0, Frame(2, 1), Frame(3, 2), Frame(4, 3)), .1);
            for (int i = 0; i <= 10; i++) buffer.BeginUpdate(.1 + i * .01);
            Assert.That(buffer.TryTake(.2, out _), Is.True);
            buffer.BeginUpdate(.5);
            Assert.That(buffer.TryTake(.5, out var frame), Is.True);
            Assert.That(frame.tick, Is.EqualTo(2));
            Assert.That(buffer.TryTake(.5, out _), Is.False);
        }
    }
}
