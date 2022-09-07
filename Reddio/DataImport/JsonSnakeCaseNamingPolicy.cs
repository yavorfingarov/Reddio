using System.Text;
using System.Text.Json;

namespace Reddio.DataImport
{
    public class JsonSnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                {
                    sb.Append('_');
                }
                sb.Append(char.ToLower(name[i]));
            }

            return sb.ToString();
        }
    }
}
