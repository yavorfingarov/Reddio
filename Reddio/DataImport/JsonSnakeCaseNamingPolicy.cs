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
                if (i == 0)
                {
                    sb.Append(char.ToLower(name[i]));
                }
                else if (char.IsUpper(name[i]))
                {
                    sb.Append($"_{char.ToLower(name[i])}");
                }
                else
                {
                    sb.Append(name[i]);
                }
            }

            return sb.ToString();
        }
    }
}
