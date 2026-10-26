using System.IO;
using Newtonsoft.Json;

namespace LocalServer
{
    /// <summary>
    /// 정적 데이터 JSON(계약 1번 형식)에서 <c>schemaVersion</c> 과 <c>staticVersion</c> 만 읽는다.
    /// 파일은 수백 KB 이므로 전체를 객체로 만들지 않고 최상위 속성만 훑으며, <c>tables</c> 는 건너뛴다.
    /// </summary>
    public static class StaticDataFile
    {
        public static StaticDataHeader ReadHeader(TextReader text)
        {
            var header = new StaticDataHeader();
            using var reader = new JsonTextReader(text) { CloseInput = false };

            if (!reader.Read() || reader.TokenType != JsonToken.StartObject) return header;

            bool hasSchema = false;
            bool hasVersion = false;
            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                string name = (string)reader.Value;
                if (!reader.Read()) break;

                switch (name)
                {
                    case "schemaVersion" when reader.TokenType == JsonToken.Integer:
                        header.SchemaVersion = System.Convert.ToInt32(reader.Value);
                        hasSchema = true;
                        break;
                    case "staticVersion" when reader.TokenType == JsonToken.String:
                        header.StaticVersion = (string)reader.Value;
                        hasVersion = true;
                        break;
                    default:
                        reader.Skip();
                        break;
                }

                if (hasSchema && hasVersion) break;
            }

            return header;
        }

        /// <summary>파일이 없거나 JSON 이 아니면 빈 값을 돌려준다.</summary>
        public static StaticDataHeader ReadHeader(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return default;

            try
            {
                using var stream = new StreamReader(path);
                return ReadHeader(stream);
            }
            catch (IOException)
            {
                return default;
            }
            catch (JsonException)
            {
                return default;
            }
        }
    }
}
