using System.IO;
using System.Text;

namespace VTS.Networking.Impl
{
    // This is an Example, put your own implementation here.
    public class TokenStorageImpl : ITokenStorage
    {
        private static readonly UTF8Encoding ENCODER = new UTF8Encoding();
        private string _fileName = "token.json";
        private string _path = "";

        public TokenStorageImpl()
        {
            _path = Path.Combine("data/", _fileName);
        }

        public string LoadToken()
        {
            return File.Exists(_path) ? File.ReadAllText(_path) : null;
        }

        public void SaveToken(string token)
        {
            File.WriteAllText(_path, token, ENCODER);
        }

        public void DeleteToken()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }
}
