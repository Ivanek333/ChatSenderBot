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
		public string Send_msg_keyboard(string peer_id, ReplyMessage message, string text = null)
		{
			string j = "";
			try
			{
				if (text == null)
					text = message.text;
				int symbols = text.Length;
				while (symbols > 0)
				{
					string tmessage = text.Substring(0, symbols > 512 ? 512 : symbols);
					text = text.Remove(0, symbols > 512 ? 512 : symbols);
					symbols = text.Length;
					Dictionary<string, string> request = new Dictionary<string, string>
					{
						{ "random_id", "0" },
						{ "peer_id", peer_id },
						{ "message", tmessage }
					};
					if (symbols <= 512)
					{
						VK.Keyboard keyboard = new VK.Keyboard
						{
							inline = message.inline,
							one_time = false,
							buttons = new List<List<VK.Button>>()
						};
						for (int i = 0; i < message.buttons.Count(); i++)
						{
							keyboard.buttons.Add(new List<VK.Button>());
							for (int l = 0; l < message.buttons[i].Count(); l++)
							{
								string color = "";
								switch (message.buttons[i][l].c)
								{
									case 'r':
										color = "negative";
										break;
									case 'g':
										color = "positive";
										break;
									case 'b':
										color = "primary";
										break;
									case 'w':
										color = "secondary";
										break;
								}
								switch (message.buttons[i][l].type)
								{
									case "text":
										keyboard.buttons[i].Add(new VK.Button(new VK.Action { type = "text", label = message.buttons[i][l].text }, color));
										break;
									case "open_link":
										keyboard.buttons[i].Add(new VK.Button(new VK.Action { type = "open_link", label = message.buttons[i][l].text, link = message.buttons[i][l].link }, color));
										break;
								}
							}
						}
						request.Add("keyboard", JsonConvert.SerializeObject(keyboard));
					}
					WebClient client = new WebClient { Encoding = Encoding.UTF8 };
					j = Request("messages.send", request);
				}
			}
			catch (Exception e)
			{
				Log("Error Messages_Send: " + e.ToString());
			}
			return j;
		}
		public string Request(string method, Dictionary<string, string> param)
		{
			string j = "";
			try
			{
				string p = "";
				foreach (var pair in param)
				{
					p += "&" + pair.Key + "=" + pair.Value;
				}
				j = webclient.DownloadString($"https://api.vk.com/method/{method}?v=5.130&access_token={token}&group_id={group_id}{p}");
			}
			catch (Exception e)
			{
				Log($"Request error - {method}: " + e.Message);
			}
			return j;
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
		public int to_mid;
		public char c;
		public string type;
		public string text;
		public string link;
	}
	public class ReplyMessage
	{
		public int mid;
		public bool inline;
		public string text;
		public List<List<Button>> buttons;
	}
	namespace VK
	{
		public class Action
		{
			public string type;
			public string label;
			public string link;
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
			public bool one_time, inline;
			public List<List<Button>> buttons;
		}
	}
}