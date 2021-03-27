using System;
using System.Collections.Generic;

namespace ChatSender2
{
	public class User
	{
		public string vkid;
		public int mid;
		public string user_token;
		public string phone;
		public string ref_id;
		//public string ref_code;
		public bool authed, got_token, got_phone, is_admin, got_ref;
		public SenderInfo sender;
		public AdderInfo adder;
		public AdminInfo adminInfo;

		public User()
		{
			vkid = "";
			mid = 1;
			user_token = "";
			phone = "";
			ref_id = "";
			authed = false;
			got_token = false;
			got_phone = false;
			is_admin = false;
			got_ref = false;
			sender = new SenderInfo();
			adder = new AdderInfo();
			adminInfo = new AdminInfo();
		}
	}
	public class SenderInfo
	{
		public bool is_on;
		public int minutes_between_send;
		public DateTime last_time;
		public DateTime last_get_time;
		public int last_cind;
		public string message;
		public int sended_messages;
		public int tarif;
		public List<VK.Chat> sender_chats;
		public List<VK.Chat> all_chats;
		public List<int> deleted_chats;
		public bool changed;
		public SenderInfo()
		{
			is_on = false;
			minutes_between_send = 60;
			last_time = DateTime.Now;
			last_get_time = DateTime.Now;
			last_cind = 0;
			message = "your_message";
			sended_messages = 0;
			tarif = 50;
			sender_chats = new List<VK.Chat>();
			all_chats = new List<VK.Chat>();
			deleted_chats = new List<int>();
			changed = false;
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
	public class AdminInfo
    {
		public List<string> refs;
		public int balance;
		public string temp_user;
		public int temp_user_ind;
		public AdminInfo()
        {
			refs = new List<string>();
			balance = 0;
			temp_user = "";
			temp_user_ind = 0;
        }
		public string refs_ToString()
        {
			string ret = "";
			for (int i = 0; i < refs.Count; i++)
            {
				ret += $"{i+1}. @id{refs[i]}\n";
            }
			return ret;
        }
    }
	public class MessageData
    {
		public List<ReplyMessage> messages;
		public int FindMind(int mid)
		{
			if (mid == -1) return -1;
			for (int i = 0; i < messages.Count; i++)
			{
				if (messages[i].mid == mid) return i;
			}
			return -1;
		}
	}
	public class Database
	{
		public UInt64 last_txnId;
		public List<User> users;
		public Dictionary<int, string> users_ids;
		
		public int FindUser(string vkid)
		{
			for (int i = 0; i < users.Count; i++)
				if (users[i].vkid == vkid) return i;
			return -1;
		}
		public Database()
		{
			users = new List<User>();
			users_ids = new Dictionary<int, string>();
			last_txnId = 0;
		}
	}
}