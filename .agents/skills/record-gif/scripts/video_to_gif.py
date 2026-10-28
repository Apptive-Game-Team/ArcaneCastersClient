#!/usr/bin/env python3
"""Encode a local recording as a measured, bounded-size animated GIF."""

import argparse
import json
import math
import os
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import tempfile


def positive(value):
    number = float(value)
    if not math.isfinite(number) or number <= 0:
        raise argparse.ArgumentTypeError('must be a positive finite number')
    return number


def nonnegative(value):
    number = float(value)
    if not math.isfinite(number) or number < 0:
        raise argparse.ArgumentTypeError('must be a nonnegative finite number')
    return number


def run(command):
    result = subprocess.run(command, capture_output=True, text=True,
                            encoding='utf-8', errors='replace', timeout=300)
    if result.returncode:
        raise RuntimeError(result.stderr[-3000:] or 'FFmpeg failed')


def gif_info(path):
    """Read GIF timing/loop metadata without Pillow or ffprobe."""
    data = path.read_bytes()
    if len(data) < 13 or data[:6] not in (b'GIF87a', b'GIF89a'):
        raise ValueError('Output is not a GIF')
    width, height = struct.unpack_from('<HH', data, 6)
    packed = data[10]
    offset = 13 + (3 * (2 ** ((packed & 7) + 1)) if packed & 128 else 0)
    frames = 0
    duration_cs = 0
    delay_cs = 0
    loop = None
    trailer = False

    def blocks(position):
        chunks = []
        while True:
            if position >= len(data):
                raise ValueError('Truncated GIF block')
            count = data[position]
            position += 1
            if count == 0:
                return position, chunks
            if position + count > len(data):
                raise ValueError('Truncated GIF data')
            chunks.append(data[position:position + count])
            position += count

    while offset < len(data):
        tag = data[offset]
        offset += 1
        if tag == 0x3B:
            trailer = True
            break
        if tag == 0x21:
            if offset >= len(data):
                raise ValueError('Truncated GIF extension')
            label = data[offset]
            offset, chunks = blocks(offset + 1)
            if label == 0xF9 and chunks:
                if len(chunks[0]) != 4:
                    raise ValueError('Invalid GIF control extension')
                delay_cs = struct.unpack_from('<H', chunks[0], 1)[0]
            if label == 0xFF and chunks and chunks[0] in (b'NETSCAPE2.0', b'ANIMEXTS1.0'):
                if len(chunks) > 1 and len(chunks[1]) >= 3 and chunks[1][0] == 1:
                    loop = struct.unpack_from('<H', chunks[1], 1)[0]
        elif tag == 0x2C:
            if offset + 9 > len(data):
                raise ValueError('Truncated GIF image descriptor')
            local_packed = data[offset + 8]
            offset += 9
            if local_packed & 128:
                offset += 3 * (2 ** ((local_packed & 7) + 1))
            offset, _ = blocks(offset + 1)  # LZW code size precedes subblocks.
            frames += 1
            duration_cs += delay_cs
            delay_cs = 0
        else:
            raise ValueError('Invalid GIF block tag')
    if not trailer or frames < 1:
        raise ValueError('Incomplete or empty GIF')
    return dict(width=width, height=height, frames=frames,
                seconds=duration_cs / 100.0, loop=loop)


def encode(args):
    source = args.input.expanduser().resolve()
    output = args.output.expanduser().resolve()
    report_path = output.with_suffix(output.suffix + '.json')
    if not source.is_file():
        raise ValueError(f'Input does not exist: {source}')
    if source == output or output.suffix.lower() != '.gif':
        raise ValueError('Use a separate .gif output path')
    if not args.overwrite and (output.exists() or report_path.exists()):
        raise ValueError('Output/report exists; choose a new name or use --overwrite')
    if args.min_width > args.width or args.min_fps > args.fps:
        raise ValueError('Quality floors must not exceed initial width/fps')
    ffmpeg = shutil.which(args.ffmpeg)
    if not ffmpeg:
        raise ValueError('FFmpeg is unavailable; set --ffmpeg to its executable')
    budget = int(args.max_mb * 1_000_000)
    if budget < 1:
        raise ValueError('Byte budget must be at least one byte')
    output.parent.mkdir(parents=True, exist_ok=True)
    input_args = ['-protocol_whitelist', 'file,pipe']
    if source.suffix.lower() == '.ffconcat':
        input_args += ['-f', 'concat', '-safe', '0']
    input_args += ['-i', str(source)]
    trim_args = ['-ss', str(args.start)] if args.start else []
    if args.duration is not None:
        trim_args += ['-t', str(args.duration)]

    narrower = max(args.min_width, int(args.width * 5 / 6))
    profiles = [
        (args.width, args.fps, 128),
        (args.width, max(args.min_fps, args.fps * 0.75), 96),
        (narrower, max(args.min_fps, args.fps * 0.625), 96),
        (narrower, args.min_fps, 96),
        (max(args.min_width, int(args.width * 0.75)), args.min_fps, 96),
        (args.min_width, args.min_fps, 64),
    ]
    profiles = list(dict.fromkeys(profiles))
    report = dict(input=str(source), output=str(output), budget_bytes=budget,
                  start_seconds=args.start, selected_seconds=args.duration,
                  success=False, attempts=[])
    with tempfile.TemporaryDirectory(prefix='record-gif-', dir=output.parent) as temp:
        candidate = Path(temp) / 'candidate.gif'
        for width, fps, colors in profiles:
            graph = (
                f"fps={fps:g},scale=w='min(iw,{width})':h=-1:flags=lanczos,split[a][b];"
                f'[a]palettegen=max_colors={colors}:stats_mode=diff[p];'
                '[b][p]paletteuse=dither=bayer:bayer_scale=5:diff_mode=rectangle'
            )
            command = [ffmpeg, '-hide_banner', '-loglevel', 'error', '-y']
            command += input_args + trim_args
            command += ['-filter_complex', graph, '-an', '-loop', '0',
                        '-gifflags', '+transdiff', str(candidate)]
            run(command)
            size = candidate.stat().st_size
            attempt = dict(max_width=width, fps=fps, colors=colors, bytes=size)
            report['attempts'].append(attempt)
            print(json.dumps(attempt), flush=True)
            if size > budget:
                continue
            info = gif_info(candidate)
            if info['frames'] < 2 or info['seconds'] <= 0 or info['loop'] != 0:
                raise ValueError('Output is not a valid looping animation')
            run([ffmpeg, '-hide_banner', '-v', 'error', '-i', str(candidate),
                 '-f', 'null', '-'])
            report.update(success=True, bytes=size, **info)
            # 기존 결과는 새 파일의 용량과 디코딩 검증이 끝난 뒤 교체한다.
            os.replace(candidate, output)
            break
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False), flush=True)
    if not report['success']:
        print('Size limit unmet at quality floors. Source and existing output preserved.', file=sys.stderr)
        return 2
    return 0


def main():
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8')
        sys.stderr.reconfigure(encoding='utf-8')
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--max-mb', type=positive, default=3)
    parser.add_argument('--width', type=int, choices=range(16, 16385), default=960, metavar='PX')
    parser.add_argument('--fps', type=positive, default=8)
    parser.add_argument('--min-width', type=int, choices=range(16, 16385), default=640, metavar='PX')
    parser.add_argument('--min-fps', type=positive, default=4)
    parser.add_argument('--start', type=nonnegative, default=0)
    parser.add_argument('--duration', type=positive)
    parser.add_argument('--ffmpeg', default='ffmpeg')
    parser.add_argument('--overwrite', action='store_true')
    args = parser.parse_args()
    try:
        return encode(args)
    except (ValueError, RuntimeError, OSError, subprocess.TimeoutExpired) as error:
        print(f'ERROR: {error}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
