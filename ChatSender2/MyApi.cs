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
    namespace VK
    {
        public class Chat : IComparable
        {
            public string peer_id;
            public string name;
            public byte mark;
            public override string ToString()
            {
                return $"[{this.peer_id}] {this.name}";
            }
            public int CompareTo(object obj)
            {
                if (obj == null) return 1;
                VK.Chat n = obj as VK.Chat;
                if (n != null)
                    return int.Parse(this.peer_id).CompareTo(int.Parse(n.peer_id));
                else throw new ArgumentException("Object is not a Chat");
            }
        }
    }
    public partial class MyApi
    {
        WebClient webclient = new WebClient() { Encoding = Encoding.UTF8 };
        public string token, group_id, path;
        public void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} {message}"); //in console
            try
            {
                File.WriteAllText($"{path}Log.txt", $"{File.ReadAllText(path + "Log.txt")} {DateTime.Now.ToString()} {message} \n"); //Log
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now.ToString()} Log error: {e.ToString()}");
            }
        }
        public JObject GetLongPoll()
        {
            JObject longpoll = new JObject();
            try
            {
                Console.WriteLine("Attempt \n");
                longpoll = JObject.Parse(webclient.DownloadString($"https://api.vk.com/method/groups.getLongPollServer?access_token={token}&group_id={group_id}&v=5.126"));
                Console.WriteLine("Ended : \n " + longpoll.ToString() + " \n");
            }
            catch (Exception e)
            {
                Log("Error get longpoll: " + e.ToString());
            }
            return longpoll;
        }
        public JObject WGetLongPoll()
        {
            JObject lp = new JObject();
            while (!lp.ToString().Contains("ts"))
            {
                lp = GetLongPoll();
                Thread.Sleep(500);
            }
            return lp;
        }
        public string GetUserId(string screen_name)
        {
            string gid = "";
            string json = webclient.DownloadString($"https://api.vk.com/method/utils.resolveScreenName?access_token={token}&screen_name={screen_name}&v=5.126");
            try
            {
                gid = JObject.Parse(json)["response"]["object_id"].ToString();
            } catch {  }
            if (string.IsNullOrWhiteSpace(gid) || string.IsNullOrEmpty(gid))
            {
                gid = "error";
                Console.WriteLine("Ошибка получения id:\n" + json);
            }
            return gid;
        }
        public string Send_msg(string peer_id, string message)
        {
            string j = "";
            try
            {
                int symbols = message.Length;
                while (symbols > 0)
                {
                    string tmessage = message.Substring(0, symbols > 512 ? 512 : symbols);
                    message = message.Remove(0, symbols > 512 ? 512 : symbols);
                    symbols = message.Length;
                    WebClient client = new WebClient { Encoding = Encoding.UTF8 };
                    j = client.DownloadString(string.Format("https://api.vk.com/method/messages.send?v=5.126&access_token={0}&peer_id={1}&message={2}&group_id={3}&random_id=0", token, peer_id, tmessage, group_id));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("send download error: " + e.ToString());
            }
            if (j.Contains("error")) Console.WriteLine("Send error: " + j);
            return j;
        }
        public string Send_user_msg(string user_token, string peer_id, string message)
        {
            string j = "";
            try
            {
                int symbols = message.Length;
                while (symbols > 0)
                {
                    string tmessage = message.Substring(0, symbols > 512 ? 512 : symbols);
                    message = message.Remove(0, symbols > 512 ? 512 : symbols);
                    symbols = message.Length;
                    WebClient client = new WebClient { Encoding = Encoding.UTF8 };
                    j = client.DownloadString(string.Format("https://api.vk.com/method/messages.send?v=5.126&access_token={0}&chat_id={1}&message={2}&random_id=0", user_token, peer_id, tmessage));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("send download error: " + e.ToString());
            }
            return j;
        }
        public string Send_split_msg(string peer_id, string message, char splitter)
        {
            string[] msgs = message.Split(splitter);
            string ret = "";
            for (int i = 0; i < msgs.Count(); i++)
            {
                ret += Send_msg(peer_id, msgs[i]) + "\n";
            }
            return ret;
        }
        public string ReplaceAll(string orig, Dictionary<string, string> words)
        {
            string ret = orig;
            foreach (var item in words)
            {
                if (item.Key != item.Value) //чтобы не вошёл в бесконечный цикл
                {
                    int ind = ret.IndexOf(item.Key);
                    while (ind > 1)
                    {
                        ret = ret.Replace(item.Key, item.Value);
                        ind = ret.IndexOf(item.Key);
                    }
                }
            }
            return ret;
        }
        public List<VK.Chat> GetChats(string user_token)
        {
            List<VK.Chat> chats = new List<VK.Chat>();
            try
            {
                string j = "";
                WebClient client = new WebClient { Encoding = Encoding.UTF8 };
                j = client.DownloadString(string.Format("https://api.vk.com/method/messages.getConversations?v=5.126&access_token={0}&count=1&filter=all", user_token));
                //Console.WriteLine(j);
                List<JToken> item_list = new List<JToken>();
                int item_count = -1, offset = 0;
                item_count = int.Parse(JObject.Parse(j)["response"]["count"].ToString());
                while (item_count > 0)
                {
                    j = client.DownloadString(string.Format("https://api.vk.com/method/messages.getConversations?v=5.126&access_token={0}&count=200&offset={1}&filter=all", user_token, offset));
                    item_list = JObject.Parse(j)["response"]["items"].ToList();
                    foreach (JToken item in item_list)
                    {
                        //Console.WriteLine("\n\n1. " + item.ToString());
                        if (item["conversation"]["peer"]["type"].ToString() == "chat")
                        {
                            //Console.WriteLine("\n\n2. " + item["conversation"].ToString());
                            chats.Add(new VK.Chat
                            {
                                peer_id = item["conversation"]["peer"]["local_id"].ToString(),
                                name = item["conversation"]["chat_settings"]["title"].ToString(),
                                mark = 0
                            });
                            //Console.WriteLine(chats[chats.Count - 1].ToString());
                        }
                    }
                    offset += 200;
                    item_count -= 200;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("get chats error: " + e.ToString());
            }
            chats.Sort();
            return chats;
        }
        public string ChatList2String(List<VK.Chat> chats)
        {
            string ret = "";
            for (int i = 0; i < chats.Count; i++)
            {
                switch (chats[i].mark)
                {
                    case 1:
                        ret += "✅";
                        break;
                    case 0:
                        ret += "⬜";
                        break;
                    case 2:
                        ret += "❌";
                        break;
                }
                ret += " " + chats[i].ToString() + "\n";
                if ((i + 1) % 15 == 0)
                    ret += "#";
            }
            return ret;
        }
        public string QiwiGet(string phone, string token)
        {
            string url = "https://edge.qiwi.com";
            url += $"/payment-history/v2/persons/{phone}/payments?rows=10&operation=IN";
            WebRequest request = WebRequest.Create(url);
            request.Method = "Get";
            request.Headers = new WebHeaderCollection();
            request.Headers.Add("Authorization", "Bearer " + token);
            request.ContentType = "application/json";
            WebResponse response = request.GetResponse();
            string lines = "";
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line = "";
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines += line;
                    }
                }
            }
            while (lines.IndexOf('\n') > 0)
            {
                lines = lines.Remove(lines.IndexOf('\n'), 1);
            }
            response.Close();
            Console.WriteLine("Qiwi request complited");
            return lines;
        }
        public string InviteUser(string chat_id, string user_id, string token)
        {
            return webclient.DownloadString($"https://api.vk.com/method/messages.addChatUser?access_token={token}&chat_id={chat_id}&user_id={user_id}&visible_messages_count=250&v=5.126");
        }
    }
}