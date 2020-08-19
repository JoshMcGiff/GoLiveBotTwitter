# GoLiveBotTwitter
This application updates the user's Twitter name, bio and profile picture when they are live on Twitch. It also resets them when they are offline.
![Example](https://i.imgur.com/C2imVnY.jpg)
## How To Use
1) Download the necessary files above. 
2) Using a C# text editor, insert the following changes to the code with your own information:
   1) Update the Twitch clientID and clientSecret (You must have a [Twitch Developer](https://dev.twitch.tv/) account).
   2) Replace streamer with your Twitch username.
   3) Update the Twitter consumerKey, consumerSecret, userAccessToken and userAccessSecret (You must have a [Twitter Developer](https://developer.twitter.com/en) account).
3) Ensure that you have installed the following NuGet packages:
   1) [TwitchLib 3.0.1](https://www.nuget.org/packages/TwitchLib/3.0.1)
   2) [TweetInvi 4.0.3](https://www.nuget.org/packages/TweetinviAPI/4.0.3/)
   3) [Newtonsoft.Json 12.0.3](https://www.nuget.org/packages/Newtonsoft.Json/12.0.3)
4) Compile and build. Use the .exe file to run the program.
