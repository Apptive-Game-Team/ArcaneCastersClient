using System;
using Global.Serialization;
using Newtonsoft.Json;

namespace Data.Magic
{
    /// <summary>indicator layer 가 그리는 도형.</summary>
    public enum MagicIndicatorShape
    {
        Unknown,
        Circle,
        Lane
    }

    /// <summary>indicator layer 의 기준점.</summary>
    public enum MagicIndicatorOrigin
    {
        Unknown,

        /// <summary>시전하는 플레이어의 위치.</summary>
        Caster,

        /// <summary>사거리로 자른 조준점.</summary>
        Target
    }

    /// <summary>lane layer 가 어디서 끝나는지.</summary>
    public enum MagicIndicatorLaneEnd
    {
        Unknown,

        /// <summary>조준점 쪽으로. 길이는 <c>length</c> 가 있으면 그 값, 없으면 마법의 cast range 다.</summary>
        Target,

        /// <summary>시전자가 보는 방향으로 <c>length</c> 만큼.</summary>
        Forward,

        /// <summary>조준점까지. 길이가 origin 과 조준점 사이 거리 그대로다.</summary>
        Aim
    }

    /// <summary>
    /// indicator document 의 layer 하나.
    /// <para>
    /// <c>shape</c>, <c>origin</c>, <c>end</c> 를 enum 이 아니라 string 으로 받는 이유가 있다.
    /// <see cref="Global.Serialization.JsonCodec"/> 에 등록된 <c>StringEnumConverter</c> 는
    /// 모르는 값을 만나면 throw 하는데, 계약상 모르는 값은 그 layer 만 건너뛰고 나머지는 그려야 한다.
    /// 그래서 문자열로 받아 <see cref="Prepare"/> 에서 한 번만 해석한다.
    /// </para>
    /// </summary>
    [Serializable]
    public class MagicIndicatorLayer
    {
        private const string ShapeCircle = "circle";
        private const string ShapeLane = "lane";
        private const string OriginCaster = "caster";
        private const string OriginTarget = "target";
        private const string LaneEndTarget = "target";
        private const string LaneEndForward = "forward";
        private const string LaneEndAim = "aim";

        public string shape;

        /// <summary>비어 있으면 <see cref="MagicIndicatorOrigin.Target"/> 이다.</summary>
        public string origin;

        /// <summary>lane 전용. <c>"target"</c>, <c>"forward"</c>, <c>"aim"</c> 중 하나.</summary>
        public string end;

        public MagicIndicatorValue radius;
        public MagicIndicatorValue forwardOffset;
        public MagicIndicatorValue edgeWidth;
        public MagicIndicatorValue length;
        public MagicIndicatorValue halfWidth;

        /// <summary>
        /// lane 전용 선택 항목. <c>false</c> 면 lane 이 조준점의 높이를 따른다. 없거나 boolean 이 아니면 true 다.
        /// boolean 이 아닌 값이 와도 throw 하지 않도록 object 로 받고 <see cref="Prepare"/> 에서 해석한다.
        /// </summary>
        public object grounded;

        // 아래 네 개는 Prepare 가 한 번 채운다. private 이라 Json.NET 이 직렬화하지 않는다.
        private MagicIndicatorShape parsedShape;
        private MagicIndicatorOrigin parsedOrigin;
        private MagicIndicatorLaneEnd parsedEnd;
        private bool parsedGrounded = true;
        private bool readable;

        // 아래 다섯 개는 Prepare 가 푼 결과를 읽기만 한다. [JsonIgnore] 가 없으면 Json.NET 이 getter 도
        // 직렬화해서 PlayerPrefs cache 에 shape 와 Shape 가 나란히 들어간다.

        /// <summary><see cref="Prepare"/> 가 이 layer 를 그릴 수 있다고 본 경우에만 true.</summary>
        [JsonIgnore]
        public bool IsReadable => readable;

        [JsonIgnore]
        public MagicIndicatorShape Shape => parsedShape;

        [JsonIgnore]
        public MagicIndicatorOrigin Origin => parsedOrigin;

        [JsonIgnore]
        public MagicIndicatorLaneEnd End => parsedEnd;

        /// <summary>false 일 때만 lane 이 조준점의 높이를 따른다. 기본은 true.</summary>
        [JsonIgnore]
        public bool Grounded => parsedGrounded;

        /// <summary>
        /// 문자열 필드를 enum 으로 해석하고, 이 layer 를 그릴 수 있는지 알려준다.
        /// document 당 한 번만 불리므로 매 프레임 문자열을 비교하지 않는다.
        /// </summary>
        /// <param name="problem">그릴 수 없을 때 왜 그런지. 그릴 수 있으면 null.</param>
        public bool Prepare(out string problem)
        {
            parsedShape = ParseShape(shape);
            parsedOrigin = ParseOrigin(origin);
            parsedEnd = ParseLaneEnd(end);
            parsedGrounded = !(grounded is bool groundedFlag) || groundedFlag;

            readable = false;

            if (parsedShape == MagicIndicatorShape.Unknown)
            {
                problem = $"unknown shape '{shape}'";
                return false;
            }

            if (parsedOrigin == MagicIndicatorOrigin.Unknown)
            {
                problem = $"unknown origin '{origin}'";
                return false;
            }

            if (parsedShape == MagicIndicatorShape.Circle && !radius.IsPresent)
            {
                problem = "circle layer has no radius";
                return false;
            }

            if (parsedShape == MagicIndicatorShape.Lane)
            {
                if (parsedEnd == MagicIndicatorLaneEnd.Unknown)
                {
                    problem = $"unknown lane end '{end}'";
                    return false;
                }

                if (!halfWidth.IsPresent)
                {
                    problem = "lane layer has no halfWidth";
                    return false;
                }

                if (parsedEnd == MagicIndicatorLaneEnd.Forward && !length.IsPresent)
                {
                    problem = "lane layer ends forward but has no length";
                    return false;
                }
            }

            problem = null;
            readable = true;
            return true;
        }

        private static MagicIndicatorShape ParseShape(string value)
        {
            if (IsSame(value, ShapeCircle))
            {
                return MagicIndicatorShape.Circle;
            }

            return IsSame(value, ShapeLane) ? MagicIndicatorShape.Lane : MagicIndicatorShape.Unknown;
        }

        private static MagicIndicatorOrigin ParseOrigin(string value)
        {
            // origin 은 선택 항목이고, 없으면 target 이다.
            if (string.IsNullOrWhiteSpace(value) || IsSame(value, OriginTarget))
            {
                return MagicIndicatorOrigin.Target;
            }

            return IsSame(value, OriginCaster) ? MagicIndicatorOrigin.Caster : MagicIndicatorOrigin.Unknown;
        }

        private static MagicIndicatorLaneEnd ParseLaneEnd(string value)
        {
            if (IsSame(value, LaneEndTarget))
            {
                return MagicIndicatorLaneEnd.Target;
            }

            if (IsSame(value, LaneEndAim))
            {
                return MagicIndicatorLaneEnd.Aim;
            }

            return IsSame(value, LaneEndForward) ? MagicIndicatorLaneEnd.Forward : MagicIndicatorLaneEnd.Unknown;
        }

        private static bool IsSame(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
