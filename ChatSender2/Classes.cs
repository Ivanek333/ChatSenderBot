using System;
using System.Collections.Generic;

//some changes 123
namespace ChatSender2
{
	public class User
	{
		public string vkid;
		public int mid;
		public string user_token;
		public string phone;
		public string ref_id;
		public bool authed, got_token, got_phone, is_admin, got_ref;
		public List<VK.Chat> sender_chats;
		public List<VK.Chat> all_chats;
		public List<int> deleted_chats;
		public SenderInfo sender;
		public AdderInfo adder;

	}
	public class SenderInfo
	{
		public bool is_on;
		public int minutes_between_send;
		public DateTime last_time;
		public int last_cind;
		public string message;
		public int sended_messages;
		public int tarif;
		public SenderInfo()
		{
			is_on = false;
			minutes_between_send = 0;
			last_time = DateTime.Now;
			last_cind = 0;
			message = "";
			sended_messages = 0;
			tarif = 50;
		}
	}
	public class AdderInfo
	{
		public bool is_on, wait;
		public DateTime last_time;
		public int last_cind;
		public AdderInfo()
		{
			is_on = false;
			wait = false;
			last_cind = 0;
			last_time = DateTime.Now;
		}
	}
	public class Database
	{
		public UInt64 last_txnId;
		public List<User> users;
		public List<ReplyMessage> messages;
		public Dictionary<int, string> users_ids;
		public int FindMind(int mid)
		{
			if (mid == -1) return -1;
			for (int i = 0; i < messages.Count; i++)
			{
				if (messages[i].mid == mid) return i;
			}
			return -1;
		}
		public int FindUser(string vkid)
		{
			for (int i = 0; i < users.Count; i++)
				if (users[i].vkid == vkid) return i;
			return -1;
		}
		public Database()
		{
			messages = new List<ReplyMessage>();
			users = new List<User>();
			users_ids = new Dictionary<int, string>();
		}
	}
}
