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
		public bool got_token, got_phone, is_admin, got_ref;
		public List<string> refs;
		public AdderInfo adder;
		public AdminInfo adminInfo;
		public SenderInfo sender;

		public User()
		{
			vkid = "";
			mid = 1;
			user_token = "";
			phone = "";
			ref_id = "";
			got_token = false;
			got_phone = false;
			is_admin = false;
			got_ref = false;
			refs = new List<string>();
			sender = new SenderInfo();
			adder = new AdderInfo();
			adminInfo = new AdminInfo();
		}
		public string refs_ToString()
		{
			string ret = "";
			for (int i = 0; i < refs.Count; i++)
			{
				ret += $"{i + 1}. @id{refs[i]}\n";
			}
			return ret;
		}
	}
	public class SenderInfo
	{
		public string message;
		public int minutes_between_send;
		public bool is_on;
		public DateTime last_time;
		public int last_cind;
		public int sended_messages;
		public int tarif;
		public bool changed;
		public List<int> sender_chats;
		public List<int> deleted_chats;
		public List<Chat> all_chats;
		public SenderInfo()
		{
			message = "введите сообщение";
			minutes_between_send = 200;
			is_on = false;
			last_time = DateTime.Now;
			last_cind = 0;
			sended_messages = 0;
			tarif = 50;
			sender_chats = new List<int>();
			all_chats = new List<Chat>();
			deleted_chats = new List<int>();
			changed = false;
		}
	}
	public class Chat : IComparable
	{
		public int cid;
		public string name;
		public byte mark;
		public override string ToString()
		{
			return $"[{this.cid.ToString()}] {this.name}";
		}
		public int CompareTo(object obj)
		{
			if (obj == null) return 1;
			Chat n = obj as Chat;
			if (n != null)
				return this.cid.CompareTo(n.cid);
			else throw new ArgumentException("Object is not a Chat");
		}
	}
	/*public class Template
	{
		public string message;
		public int minutes_between_send;
		public Template()
		{
			message = "пустой";
			minutes_between_send = 0;
		}
	}*/
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
		public int balance;
		public string temp_user;
		public int temp_user_ind;
		public AdminInfo()
		{
			balance = 0;
			temp_user = "";
			temp_user_ind = 0;
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

	public class Tokens
	{
		public string group_id;
		public string group_token;
		public string my_id;
		public string adder_id;
		public string qiwi_token;
		public string qiwi_phone;
		public Tokens()
		{
			group_id = "";
			group_token = "";
			my_id = "";
			adder_id = "";
			qiwi_token = "";
			qiwi_phone = "";
		}
	}
	public class Database
	{
		public UInt64 last_txnId;
		public List<string> users_ids;
		public List<User> users;

		public int FindUser(string vkid)
		{
			for (int i = 0; i < users.Count; i++)
				if (users[i].vkid == vkid) return i;
			return -1;
		}
		public Database()
		{
			users = new List<User>();
			users_ids = new List<string>();
			last_txnId = 0;
		}
	}
}