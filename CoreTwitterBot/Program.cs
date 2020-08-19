using System;
using TwitchLib.Api;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Tweetinvi;
using System.Drawing;
using Tweetinvi.Parameters;
using Tweetinvi.Models;
using System.Threading;

namespace ChatBot
{
    public class Data
    {
        public string game_id { get; set; }
        public string id { get; set; }
        public string language { get; set; }
        public string started_at { get; set; }
        public string[] tag_ids { get; set; }
        public string thumbnail_url { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string user_id { get; set; }
        public string user_name { get; set; }
        public int viewer_count { get; set; }

    }
    public class TwitchFile
    {
        public Data[] data { get; set; }
    }
    class Program
    {
        string clientID = ""; // Insert your Twitch client ID here:
        string clientSecret = ""; // Insert your Twitch client secret here:
        string streamer = "streamerName";

        private static TwitchAPI api;
        IAuthenticatedUser twitterUsername; 
        string savedDescription;
        bool nameReset;
        bool nameChanged;
        bool pictureReset;
        bool pictureChanged;
        Image original;

        Program()
        {
            Auth.SetUserCredentials("", "", "", ""); // Insert your Twitter: consumerKey, consumerSecret, userAccessToken and userAccessSecret in this order:
            twitterUsername = User.GetAuthenticatedUser();
            var url = string.IsNullOrEmpty(twitterUsername.UserDTO.ProfileImageUrlHttps) ? twitterUsername.UserDTO.ProfileImageUrl : twitterUsername.UserDTO.ProfileImageUrlHttps;
            url = url.Replace("_normal", "");
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(new Uri(url),  @"profilepicture.jpg");
                original = Image.FromFile("profilepicture.jpg");
            }
            savedDescription = twitterUsername.Description;
            nameReset = false;
            nameChanged = false;
            pictureReset = false;
            pictureChanged = false;
        }

        static void Main(string[] args)
        {
            Program instance = new Program();

            while (true)
            {
                if (instance.checkIfOnline())
                {
                    instance.ChangeBioAndName();
                    instance.ChangeProfilePictureTwitter();
                }

                else
                {
                    instance.ResetBioAndName();
                    instance.ResetProfilePictureTwitter();
                }
                Thread.Sleep(600000);
            }
        }

        private void ChangeProfilePictureTwitter()
        {
            if (pictureChanged)
                return;

            pictureReset = false;
            pictureChanged = true;
            var profilePicStream = twitterUsername.GetProfileImageStream();
            var tempStream = new MemoryStream();
            profilePicStream.CopyTo(tempStream);
            byte[] profilePic = new byte[tempStream.Length];
            Rectangle portionOfImage = new Rectangle(0, 0, 400, 400);

            profilePicStream.Read(profilePic, 0, (int)tempStream.Length);
            Image currentProfilePicture = Image.FromFile( "profilepicture.jpg");
            Image liveUpdateTemplate = Image.FromFile("CoreTwitterBot/CoreTwitterBot/redcircle.png");
            using (Graphics grfx = Graphics.FromImage(currentProfilePicture))
            {
                grfx.DrawImage(liveUpdateTemplate, portionOfImage);
            }
            System.IO.MemoryStream newStream = new System.IO.MemoryStream();
            currentProfilePicture.Save(newStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            byte[] xByte = ReadToEnd(newStream);
            Account.UpdateProfileImage(xByte);
        }
        public static byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        private void ResetProfilePictureTwitter()
        {
            if (pictureReset)
                return;

            pictureReset = false;
            pictureChanged = true;
            System.IO.MemoryStream newStream = new System.IO.MemoryStream();
            original.Save(newStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            byte[] xByte = ReadToEnd(newStream);
            Account.UpdateProfileImage(xByte);

        }

        private bool checkIfOnline()
        {

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.MissingMemberHandling = MissingMemberHandling.Ignore;
            api = new TwitchAPI();
            api.Settings.ClientId = clientID;
            api.Settings.Secret = clientSecret;
            api.Settings.SkipAutoServerTokenGeneration = false;
            api.Settings.AccessToken = GetAccessToken();

            String response = Get("https://api.twitch.tv/helix/streams?user_login=" + streamer); //Change to your Twitch username
            Console.WriteLine(response); 
            TwitchFile parsedFile = JsonConvert.DeserializeObject<TwitchFile>(response, settings);
            if (parsedFile.data.Length > 0)
            {
                Console.WriteLine("Currently Online" + parsedFile.data[0].type);
                return true;
            }
            else
            {
                Console.WriteLine("Currently Offline");
                return false;
            }
        }

        private void ChangeProfilePicture()
        {
            var profilePicStream = twitterUsername.GetProfileImageStream(Tweetinvi.Models.ImageSize.normal);
            byte[] profilePic = new byte[profilePicStream.Length];
            profilePicStream.Read(profilePic, 0, (int)profilePicStream.Length);
            Image currentProfilePicture = System.Drawing.Image.FromStream(profilePicStream);
            Image liveUpdateTemplate = Image.FromFile("CoreTwitterBot/CoreTwitterBot/redcircle.png");


            using (Graphics grfx = Graphics.FromImage(currentProfilePicture))
            {
                grfx.DrawImage(liveUpdateTemplate, 0, 0);
            }

        }

        private void ChangeBioAndName()
        {
            if (nameChanged)
                return;

            nameReset = false;
            nameChanged = true;
            var accountParams = new AccountUpdateProfileParameters
            {
                Name = "🔴Live ttv/" + streamer,
                Description = "🔴Live Now: twitch.tv/" + streamer + "\n + savedDescription //Ensure that your description + the live update is less than the max (160 characters)
            };
            Account.UpdateAccountProfile(accountParams);
        }

        private void ResetBioAndName()
        {
            if (nameReset)
                return;

            nameReset = true;
            nameChanged = false;
            var accountParams = new AccountUpdateProfileParameters
            {
                Name = streamer,
                Description = savedDescription
            };
            Account.UpdateAccountProfile(accountParams);
        }

        public string GetAccessToken()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://id.twitch.tv/oauth2/token?");

            string postData = "client_id=" + Uri.EscapeDataString(clientID);
            postData += "&client_secret=" + Uri.EscapeDataString(clientSecret);
            postData += "&grant_type=client_credentials";
            byte[] data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (System.IO.Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (System.IO.Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                Console.WriteLine("\n" + json + "\n");
                string json_key = "{\"access_token\":\"";
                int index = json.IndexOf(json_key, 0, json.Length); 
                if (index != -1)
                {
                    int endpos = json.IndexOf(',', 0, json.Length);
                    if (endpos != -1)
                    {
                        int startpos = index + json_key.Length;
                        string token = json.Substring(startpos, endpos - json_key.Length - 1);
                        Console.WriteLine("\n" + token + "\n");
                        return token;
                    }
                }
                return "";
            }
        }

        public string Get(string uri)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Headers["Client-Id"] = clientID;
                request.Headers["Authorization"] = "Bearer " + api.Settings.AccessToken;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (System.IO.Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException e)
            {
                Console.WriteLine("WebException Message:\n" + e.Message);

                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
                    using (System.IO.Stream data = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(data))
                    {
                        Console.WriteLine(reader.ReadToEnd());
                    }
                }
                return "";
            }

        }

    }
}
