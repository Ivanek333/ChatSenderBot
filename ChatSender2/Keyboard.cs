using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace ChatSender2
{
	public partial class MyApi
	{
		public int FindMidByText(ReplyMessage replyMessage, string text)
		{
			for (int i = 0; i < replyMessage.buttons.Count(); i++)
			{
				for (int j = 0; j < replyMessage.buttons[i].Count(); j++)
				{
					if (replyMessage.buttons[i][j].text == text) return replyMessage.buttons[i][j].to_mid;
				}
			}
			return -1;
		}
		public string Send_msg_keyboard(string peer_id, ReplyMessage keyboard, string message = null)
		{
			string json = "", url = "-1";
			try
			{
				if (message == null) message = keyboard.text;
				int symbols = message.Length;
				while (symbols > 0)
				{
					string tmessage = message.Substring(0, symbols > 1000 ? 1000 : symbols);
					message = message.Remove(0, symbols > 1000 ? 1000 : symbols);
					symbols = message.Length;
					url = $"https://api.vk.com/method/messages.send?v=5.126&access_token={token}&peer_id={peer_id}&message={tmessage}&group_id={group_id}&random_id=0";
					if (symbols <= 512)
					{
						VK.Keyboard vkkeyboard = new VK.Keyboard
						{
							one_time = false,
							buttons = new List<List<VK.Button>>()
						};
						for (int i = 0; i < keyboard.buttons.Count(); i++)
						{
							vkkeyboard.buttons.Add(new List<VK.Button>());
							for (int j = 0; j < keyboard.buttons[i].Count(); j++)
							{
								string color = "";
								switch (keyboard.buttons[i][j].c)
								{
									case 1:
										color = "secondary";
										break;
									case 2:
										color = "primary";
										break;
									case 3:
										color = "negative";
										break;
									case 4:
										color = "positive";
										break;
								}
								vkkeyboard.buttons[i].Add(new VK.Button(new VK.Action("text", keyboard.buttons[i][j].text), color));
							}
						}
						string keys_json = JsonConvert.SerializeObject(vkkeyboard);
						url = url + "&keyboard=" + keys_json;
					}
					//Console.WriteLine(url);
					WebClient client = new WebClient { Encoding = Encoding.UTF8 };
					json = client.DownloadString(url);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("send download error: " + e.Message + "\n" + json + "\n");
				Console.WriteLine(url);
			}
			if (json.Contains("error")) Console.WriteLine("Send error: " + json);
			return json;
		}
		public string Send_split_msg_keyboard(string peer_id, ReplyMessage keyboard, char splitter, string message = null)
		{
			string[] msgs = keyboard.text.Split(splitter);
			if (message != null)
				msgs = message.Split(splitter);
			string ret = "";
			for (int i = 0; i < msgs.Count() - 1; i++)
			{
				ret += Send_msg(peer_id, msgs[i]) + "\n";
			}
			ret += Send_msg_keyboard(peer_id, keyboard, msgs[msgs.Count() - 1]);
			return ret;
		}
	}
	public class Button
	{
		public string text;
		public int to_mid;
		public int c;
		public Button(string _text, int _to_mid, int _c = 1)
		{
			text = _text;
			to_mid = _to_mid;
			c = _c;
		}
	}
	public class ReplyMessage
	{
		public int mid;
		public string text;
		public List<List<Button>> buttons;
	}
	namespace VK
	{
		public class Action
		{
			public string type;
			public string label;
			public Action(string _type, string _label)
			{
				type = _type;
				label = _label;
			}
		}
		public class Button
		{
			public Action action;
			public string color;
			public Button(Action _action, string _color)
			{
				action = _action;
				color = _color;
			}
		}
		public class Keyboard
		{
			public bool one_time;
			public List<List<Button>> buttons;
		}
	}
}