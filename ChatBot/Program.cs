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
        private static TwitchAPI api;
        IAuthenticatedUser streamer;
        string savedDescription;
        bool nameReset = false;
        bool nameChanged = false;
        bool pictureReset = false;
        bool pictureChanged = false;
        Image original = Image.FromFile("SAVE THE ORIGINAL PROFILE PICTURE");

        Program()
        {
            Auth.SetUserCredentials(TwitterInfo.ConsumerKey, TwitterInfo.ConsumerSecret, TwitterInfo.UserAcessToken, TwitterInfo.UserAcessSecret);
            streamer = User.GetAuthenticatedUser();
            savedDescription = "Sample Twitter Bio";
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
                }else
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
            var url = string.IsNullOrEmpty(streamer.UserDTO.ProfileImageUrlHttps) ? streamer.UserDTO.ProfileImageUrl : streamer.UserDTO.ProfileImageUrlHttps;
            url = url.Replace("_normal", "");
            Console.WriteLine(url);

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(new Uri(url), @"Desktop/profilepicture.jpg");
            }

            var profilePicStream = streamer.GetProfileImageStream();
            var tempStream = new MemoryStream();
            profilePicStream.CopyTo(tempStream);
            byte[] profilePic = new byte[tempStream.Length];
            Rectangle portionOfImage = new Rectangle(0, 0, 400, 400); // Set the portion of the overlaying image that you would like to add to your profile picture when you are "live".

            profilePicStream.Read(profilePic, 0, (int)tempStream.Length);
            Image currentProfilePicture = Image.FromFile("Desktop/profilepicture.jpg"); //*THESE ARE EXAMPLE FILEPATHS - REPLACE WITH YOUR OWN IMAGES
            Image liveUpdateTemplate = Image.FromFile("Desktop/redcircle.png"); //*THESE ARE EXAMPLE FILEPATHS - REPLACE WITH YOUR OWN IMAGES


            using (Graphics grfx = Graphics.FromImage(currentProfilePicture))
            {
                grfx.DrawImage(liveUpdateTemplate, portionOfImage);
            }
            ImageConverter imageConverter = new ImageConverter();
            byte[] xByte = (byte[])imageConverter.ConvertTo(currentProfilePicture, typeof(byte[]));


            Account.UpdateProfileImage(xByte);
        }

        private void ResetProfilePictureTwitter()
        {
            if (pictureReset)
                return;

            pictureReset = true;
            pictureChanged = false;

            ImageConverter imageConverter = new ImageConverter();
            byte[] xByte = (byte[])imageConverter.ConvertTo(original, typeof(byte[]));


            Account.UpdateProfileImage(xByte);

        }
        private bool checkIfOnline()
        {

            JsonSerializerSettings settings = new JsonSerializerSettings();

            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.MissingMemberHandling = MissingMemberHandling.Ignore;

            api = new TwitchAPI();
            api.Settings.ClientId = TwitchInfo.ClientId;
            api.Settings.Secret = TwitchInfo.ClientSecret;
            api.Settings.SkipAutoServerTokenGeneration = false;
            api.Settings.AccessToken = GetAccessToken();

            String response = Get("https://api.twitch.tv/helix/streams?user_login=streamer"); //Change streamer to your Twitch username.
            Console.WriteLine(response); 
            TwitchFile parsedFile = JsonConvert.DeserializeObject<TwitchFile>(response, settings);
            if (parsedFile.data.Length > 0)
            {
                return true;
            }
            else
            {
                return false;
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
                Name = "🔴Live ttv/streamer", //*REPLACE WITH YOUR OWN USER NAME
                Description = "🔴Live Now: twitch.tv/streamer \n" + savedDescription
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
                Name = "streamer", // This is the default Twitter username when offline
                Description = savedDescription
            };
            Account.UpdateAccountProfile(accountParams);
        }

        public string GetAccessToken()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://id.twitch.tv/oauth2/token?");

            string postData = "client_id=" + Uri.EscapeDataString(TwitchInfo.ClientId);
            postData += "&client_secret=" + Uri.EscapeDataString(TwitchInfo.ClientSecret);
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
                request.Headers["Client-Id"] = TwitchInfo.ClientId;
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
