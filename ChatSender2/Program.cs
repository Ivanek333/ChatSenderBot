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
    public class Program
    {
        public static void Main()
        {
            string my_vk_id = @"320317706";
            string token = @"941e434cf701f65a10ddca02e305ee0c38b30034ec2c7d9f8260615795d4f040399a31a083c54b5d675b4";
            string path = @"C:/BotsFiles/ChatSender2/";
            string group_id = @"203082034";
            Random rand = new Random();
            MyApi api = new MyApi
            {
                token = token,
                group_id = group_id,
                path = path
            };
            Database data = new Database();
            MessageData mdata = new MessageData();
            try
            {
                JsonConvert.PopulateObject(File.ReadAllText($"MessageData.json"), mdata);
                JsonConvert.PopulateObject(File.ReadAllText($"{path}Database.json"), data);
                foreach (var item in data.users_ids)
                {
                    User temp = new User();
                    JsonConvert.PopulateObject(File.ReadAllText($"{path}Users_data/{item.Value}.json"), temp);
                    data.users.Add(temp);
                }
            }
            catch (Exception e)
            {
                api.Log("Database parse error" + e.ToString());
                return;
            }
            try
            {
                foreach (var item in data.users)
                {
                    if (item.ref_id == null) item.ref_id = "";
                    if (item.adder == null) item.adder = new AdderInfo();
                    //if (item. == null) item. = ;
                    File.WriteAllText($"{path}Users_data/{item.vkid}.json", JsonConvert.SerializeObject(item));
                }
            }
            catch (Exception e)
            {
                api.Log("Database rewrite error" + e.ToString());
                return;
            }
            WebClient webclient = new WebClient { Encoding = Encoding.UTF8 };
            JObject responseLongPoll;
            string json = "", url = "", ts = "";
            var col = new List<JToken>();
            responseLongPoll = api.WGetLongPoll();
            int ts_get_mins = DateTime.Now.Minute;
            int counter_main = 0;
            while (true)
            {
                if (counter_main >= 13)
                {
                    Console.WriteLine(DateTime.Now.ToLongTimeString() + " - main alive");
                    counter_main = 0;
                }
                #region doing_staff
                if ((DateTime.Now.Minute + 60 - ts_get_mins) % 60 >= 50)
                {
                    ts_get_mins = DateTime.Now.Minute;
                    responseLongPoll = api.WGetLongPoll();
                    ts = responseLongPoll["response"]["ts"].ToString();
                    try
                    {
                        File.WriteAllText($"{path}l_ts", ts);
                    }
                    catch (Exception e)
                    {
                        api.Log("Error writing ts" + e.ToString());
                    }
                    continue;
                }
                try { ts = File.ReadAllText($"{path}l_ts").Trim(); }
                catch (Exception e)
                {
                    api.Log("Error read ts: " + e.ToString());
                }
                url = string.Format("{0}?act=a_check&key={1}&wait=3&ts={2}&version=3",
                     responseLongPoll["response"]["server"].ToString(),
                     responseLongPoll["response"]["key"].ToString(),
                     ts != "" ? ts : responseLongPoll["response"]["ts"].ToString()
                     );
                try
                {
                    json = webclient.DownloadString(url);
                }
                catch (Exception e)
                {
                    api.Log(("Error downloading json: " + json + " " + e.ToString()));
                    Thread.Sleep(1000);
                    continue;
                }
                if (json.Contains("ts"))
                {
                    ts = JObject.Parse(json)["ts"].ToString();
                    try
                    {
                        File.WriteAllText($"{path}l_ts", ts);
                    }
                    catch (Exception e)
                    {
                        api.Log("Error writing ts" + e.ToString());
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                    continue;
                }
                if (JObject.Parse(json).ContainsKey("failed"))
                {
                    api.Log("json failed error: " + json.ToString());
                    responseLongPoll = api.WGetLongPoll();
                    continue;
                }
                if (json.Contains("error"))
                {
                    api.Log("json error error: " + json);
                    Thread.Sleep(1000);
                    continue;
                }

                if (!json.Contains("updates"))
                {
                    api.Log("Not found updates: " + json);
                }
                else
                {
                    #endregion
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    col = JObject.Parse(json)["updates"].ToList();
                    foreach (var item in col)
                    {
                        if (item["type"].ToString() == "message_new")
                        {
                            //Console.WriteLine(item.ToString());
                            string msg = item["object"]["message"]["text"].ToString().Trim();        //исходное сообщение
                            string good_msg = msg.ToLower();                           //улучшенная версия сообщения
                            string peer_id = item["object"]["message"]["peer_id"].ToString(); //id назначения
                            string from_id = item["object"]["message"]["from_id"].ToString(); //id отправителя
                            string message_id = item["object"]["message"]["id"].ToString();   //id сообщения											  //string message_id = item["object"]["apisage"]["id"].ToString();   //id сообщения
                            api.Log($"@id{from_id} in vk.com/gim{group_id}?sel={peer_id} : '{msg}' , good_msg: '{good_msg}'");
                            bool new_user = false;
                            int ind = data.FindUser(from_id);
                            if (ind == -1)
                            {
                                new_user = true;
                                data.users.Add(new User
                                {
                                    vkid = from_id,
                                    mid = 1,
                                    authed = false,
                                    got_token = false,
                                    got_ref = false,
                                    ref_id = "",
                                    user_token = "",
                                    sender_chats = new List<VK.Chat>(),
                                    all_chats = new List<VK.Chat>(),
                                    deleted_chats = new List<int>(),
                                    sender = new SenderInfo
                                    {
                                        last_cind = 0,
                                        last_time = DateTime.Now,
                                        message = "your_message",
                                        sended_messages = 0,
                                        tarif = 50,
                                        minutes_between_send = 0,
                                        is_on = false
                                    },
                                    adder = new AdderInfo
                                    {
                                        is_on = false,
                                        wait = false,
                                        last_time = DateTime.Now,
                                        last_cind = 0
                                    }
                                });
                                ind = data.users.Count - 1;
                                data.users_ids.Add(ind, from_id);
                                File.WriteAllText($"{path}Database.json", JsonConvert.SerializeObject(new Database
                                {
                                    last_txnId = data.last_txnId,
                                    users_ids = data.users_ids,
                                    users = new List<User>()
                                }));
                                File.Create($"{path}Users_data/{data.users_ids[ind]}.json").Close();
                                File.WriteAllText($"{path}Users_data/{data.users_ids[ind]}.json", JsonConvert.SerializeObject(data.users[ind]));
                            }
                            int cur_mid = data.users[ind].mid;
                            int cur_mind = mdata.FindMind(cur_mid);
                            int to_mid = cur_mid;
                            int to_mind = cur_mind; // can be -1
                            Dictionary<string, string> dictionary = new Dictionary<string, string>
                            {
                                { "[user_id]", from_id },
                                { "[user_token]", data.users[ind].user_token }
                            };
                            if (!new_user)
                            {
                                to_mid = api.FindMidByText(mdata.messages[cur_mind], msg);
                                to_mind = mdata.FindMind(to_mid);
                            }
                            //добавления для некоторых пунктов
                            if (to_mid == 7 && data.users[ind].is_admin)
                            {
                                to_mid = 38;
                                to_mind = mdata.FindMind(to_mid);
                            }
                            if (to_mid == 12)
                            {
                                dictionary.Add("[sender_chats_count]", data.users[ind].sender_chats.Count.ToString());
                            }
                            if (to_mid == 13)
                            {
                                dictionary.Add("[sender_chats_on]", api.ChatList2String(data.users[ind].sender_chats));
                            } 
                            if (to_mid == 14)
                            {
                                api.Send_msg(peer_id, "Подождите пожалуйста, прогружаем список всех ваших бесед...");
                                data.users[ind].all_chats = api.GetChats(data.users[ind].user_token);
                                int t = 0;
                                if (data.users[ind].deleted_chats.Count > 0)
                                    for (int i = 0; i < data.users[ind].all_chats.Count; i++)
                                    {
                                        while ((data.users[ind].deleted_chats[t] < int.Parse(data.users[ind].all_chats[i].peer_id)) && (t < data.users[ind].deleted_chats.Count - 1))
                                            t++;
                                        //Console.WriteLine(t);
                                        if (data.users[ind].all_chats[i].peer_id == data.users[ind].deleted_chats[t].ToString())
                                            data.users[ind].all_chats[i].mark = 2;
                                    }
                                t = 0;
                                if (data.users[ind].sender_chats.Count > 0)
                                    for (int i = 0; i < data.users[ind].all_chats.Count; i++)
                                    {
                                        while ((int.Parse(data.users[ind].sender_chats[t].peer_id) < int.Parse(data.users[ind].all_chats[i].peer_id)) && (t < data.users[ind].sender_chats.Count - 1))
                                            t++;
                                        //Console.WriteLine(t);
                                        if (data.users[ind].all_chats[i].peer_id == data.users[ind].sender_chats[t].peer_id)
                                            data.users[ind].all_chats[i].mark = 1;
                                    }
                                dictionary.Add("[sender_chats_all]", api.ChatList2String(data.users[ind].all_chats));
                            }
                            if (to_mid == 20 || to_mid == 28)
                            {
                                dictionary.Add("[sender_msg_count]", data.users[ind].sender.sended_messages.ToString());
                                dictionary.Add("[sender_tarif]", data.users[ind].sender.tarif.ToString());
                                dictionary.Add("[sender_left_count]", (data.users[ind].sender.tarif - data.users[ind].sender.sended_messages).ToString());
                            }
                            if (to_mid == 21)
                            {
                                dictionary.Add("[sender_message]", data.users[ind].sender.message);
                            }
                            if (to_mid == 24)
                            {
                                data.users[ind].sender.is_on = !data.users[ind].sender.is_on; 
                            } // potential error
                            if (to_mid == 20 || to_mid == 24)
                            {
                                dictionary.Add("[sender_state]", data.users[ind].sender.is_on ? "включена" : "выключена");
                            }
                            // исключения для блокирования переходов по меню
                            if ((cur_mid >= 7) && !data.users[ind].authed)
                            {
                                api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(3)], '#');
                                data.users[ind].mid = 3;
                            }
                            else if ((to_mid == 20 || to_mid == 34 || to_mid == 12 || to_mid == 7) && !data.users[ind].got_token)
                            {
                                api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(8)], '#');
                                data.users[ind].mid = 8;
                            }
                            else if ((to_mid == 30) && !data.users[ind].got_phone)
                            {
                                api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(32)], '#');
                                data.users[ind].mid = 32;
                            }
                            else if ((to_mid == 34) && !data.users[ind].got_phone)
                            {
                                api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(36)], '#');
                                data.users[ind].mid = 36;
                            }
                            else if (to_mind != -1) //если нажал на кнопку и исключений нет
                            {
                                data.users[ind].mid = mdata.messages[to_mind].mid;
                                api.Send_split_msg_keyboard(peer_id, mdata.messages[to_mind], '#', api.ReplaceAll(mdata.messages[to_mind].text, dictionary));
                            }
                            //исключения для тех пунктов, в которых нужен ввод
                            else if (cur_mid == 3)
                            {
                                if (msg == "test_code")
                                {
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(6)], '#');
                                    data.users[ind].mid = 6;
                                    data.users[ind].is_admin = false;
                                    data.users[ind].authed = true;
                                }
                                else if (msg == "admin_code")
                                {
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(6)], '#');
                                    api.Send_msg(peer_id, "Вы админ");
                                    data.users[ind].sender.tarif = 1000000;
                                    data.users[ind].mid = 6;
                                    data.users[ind].is_admin = true;
                                    data.users[ind].authed = true;
                                }
                                else
                                {
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(5)], '#');
                                    data.users[ind].authed = false;
                                }
                            } // авторизация
                            else if (cur_mid == 9)
                            {
                                if (msg.Contains("access_token=") && msg.Contains("expires_in=0"))
                                {
                                    data.users[ind].user_token = msg.Substring(msg.IndexOf("access_token=") + 13, msg.IndexOf("expires_in=0") - msg.IndexOf("access_token=") - 14);
                                    data.users[ind].got_token = true;
                                    data.users[ind].mid = 10;
                                    api.Send_msg(peer_id, data.users[ind].user_token);
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(10)], '#');
                                }
                                else
                                {
                                    data.users[ind].got_token = false;
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(11)], '#');
                                }
                            } // ввод токена
                            else if (cur_mid == 15)
                            {
                                if (!good_msg.Contains('-'))
                                {
                                    int cid = -1;
                                    if (int.TryParse(good_msg, out cid))
                                    {
                                        int ind_all = -1, ind_send = -1;
                                        for (int i = 0; i < data.users[ind].all_chats.Count; i++)
                                        {
                                            if (int.Parse(data.users[ind].all_chats[i].peer_id) == cid)
                                            {
                                                ind_all = i;
                                                break;
                                            }
                                        }
                                        if (ind_all != -1)
                                        {
                                            for (int i = 0; i < data.users[ind].sender_chats.Count; i++)
                                            {
                                                if (int.Parse(data.users[ind].sender_chats[i].peer_id) == cid)
                                                {
                                                    ind_send = i;
                                                    break;
                                                }
                                            }
                                            for (int i = 0; i < data.users[ind].deleted_chats.Count; i++)
                                            {
                                                if (data.users[ind].deleted_chats[i] == cid)
                                                {
                                                    data.users[ind].deleted_chats.RemoveAt(i);
                                                    break;
                                                }
                                            }
                                            if (ind_send == -1)
                                            {
                                                data.users[ind].all_chats[ind_all].mark = 1;
                                                data.users[ind].sender_chats.Add(data.users[ind].all_chats[ind_all]);
                                                data.users[ind].sender_chats.Sort();
                                                api.Send_split_msg_keyboard(peer_id, mdata.messages[cur_mind], '#', mdata.messages[mdata.FindMind(17)].text);
                                            }
                                            else
                                            {
                                                data.users[ind].all_chats[ind_all].mark = 0;
                                                data.users[ind].sender_chats.RemoveAt(ind_send);
                                                api.Send_split_msg_keyboard(peer_id, mdata.messages[cur_mind], '#', mdata.messages[mdata.FindMind(18)].text);
                                            }
                                        }
                                        else
                                        {
                                            api.Send_split_msg_keyboard(peer_id, mdata.messages[cur_mind], '#', mdata.messages[mdata.FindMind(16)].text);
                                        }
                                    }
                                    else
                                    {
                                        api.Send_split_msg_keyboard(peer_id, mdata.messages[cur_mind], '#', mdata.messages[mdata.FindMind(16)].text);
                                    }
                                }
                                else
                                {
                                    int cid_from = -1, cid_to = -1;
                                    if (int.TryParse(good_msg.Split('-')[0], out cid_from) && int.TryParse(good_msg.Split('-')[1], out cid_to))
                                    {
                                        List<int> chats_added = new List<int>(), chats_removed = new List<int>();
                                        for (int cid = cid_from; cid <= cid_to; cid++)
                                        {
                                            int ind_all = -1, ind_send = -1;
                                            for (int i = 0; i < data.users[ind].all_chats.Count; i++)
                                            {
                                                if (int.Parse(data.users[ind].all_chats[i].peer_id) == cid)
                                                {
                                                    ind_all = i;
                                                    break;
                                                }
                                            }
                                            if (ind_all != -1)
                                            {
                                                for (int i = 0; i < data.users[ind].sender_chats.Count; i++)
                                                {
                                                    if (int.Parse(data.users[ind].sender_chats[i].peer_id) == cid)
                                                    {
                                                        ind_send = i;
                                                        break;
                                                    }
                                                }
                                                if (ind_send == -1)
                                                {
                                                    for (int i = 0; i < data.users[ind].deleted_chats.Count; i++)
                                                    {
                                                        if (data.users[ind].deleted_chats[i] == cid)
                                                        {
                                                            data.users[ind].deleted_chats.RemoveAt(i);
                                                            break;
                                                        }
                                                    }
                                                    data.users[ind].all_chats[ind_all].mark = 1;
                                                    data.users[ind].sender_chats.Add(data.users[ind].all_chats[ind_all]);
                                                    chats_added.Add(cid);
                                                }
                                                else
                                                {
                                                    data.users[ind].all_chats[ind_all].mark = 0;
                                                    data.users[ind].sender_chats.RemoveAt(ind_send);
                                                    chats_removed.Add(cid);
                                                }
                                            }
                                        }
                                        data.users[ind].sender_chats.Sort();
                                        string str_chats_added = "", str_chats_removed = "";
                                        foreach (int id in chats_added)
                                            str_chats_added += id.ToString() + ", ";
                                        if (str_chats_added.Length > 2)
                                            str_chats_added = str_chats_added.Remove(str_chats_added.Length - 2, 2);
                                        foreach (int id in chats_removed)
                                            str_chats_removed += id.ToString() + ", ";
                                        if (str_chats_removed.Length > 2)
                                            str_chats_removed = str_chats_removed.Remove(str_chats_removed.Length - 2, 2);
                                        dictionary.Add("[sender_chats_added]", str_chats_added);
                                        dictionary.Add("[sender_chats_removed]", str_chats_removed);
                                        api.Send_split_msg_keyboard(peer_id, mdata.messages[cur_mind], '#', api.ReplaceAll(mdata.messages[mdata.FindMind(19)].text, dictionary));
                                    }
                                    else
                                    {
                                        api.Send_split_msg_keyboard(peer_id, mdata.messages[cur_mind], '#', mdata.messages[mdata.FindMind(16)].text);
                                    }
                                }
                            } // добавление бесед
                            else if (cur_mid == 22)
                            {
                                data.users[ind].sender.message = msg;
                                data.users[ind].mid = 23;
                                api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(23)], '#');
                            } // текст рассылки
                            else if (cur_mid == 25)
                            {
                                int time = -1;
                                if (int.TryParse(good_msg, out time))
                                {
                                    data.users[ind].sender.minutes_between_send = time;
                                    data.users[ind].mid = 26;
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(26)], '#');
                                }
                                else
                                {
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[cur_mind], '#', mdata.messages[mdata.FindMind(27)].text);
                                }
                            } // время между рассылками
                            else if (cur_mid == 32)
                            {
                                data.users[ind].phone = msg.Trim();
                                data.users[ind].got_phone = true;
                                data.users[ind].mid = 33;
                                api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(33)], '#');
                            } // номер телефона
                            else if (cur_mid == 36)
                            {
                                data.users[ind].phone = msg.Trim();
                                data.users[ind].got_phone = true;
                                data.users[ind].mid = 37;
                                api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(37)], '#');
                            } // номер телефона
                            else if (cur_mid == 40)
                            {
                                string add_user_id = good_msg;
                                if (add_user_id.Contains("vk.com"))
                                {
                                    add_user_id = add_user_id.Substring(add_user_id.IndexOf("vk.com") + 7, add_user_id.Length - add_user_id.IndexOf("vk.com") - 7);
                                    Console.WriteLine(add_user_id);
                                    Thread.Sleep(10);
                                    add_user_id = api.GetUserId(add_user_id);
                                    Console.WriteLine(add_user_id);
                                    if (add_user_id != "")
                                    {
                                        if (add_user_id == "group")
                                        {
                                            api.Send_msg(peer_id, "Это не человек, а группа!\nЗа приглашения групп банят в беседах, поэтому с ними не работаем!");
                                        }
                                        else
                                        {
                                            data.users.Add(new User
                                            {
                                                vkid = add_user_id,
                                                mid = 1,
                                                authed = false,
                                                got_token = false,
                                                got_ref = true,
                                                ref_id = from_id,
                                                user_token = "",
                                                sender_chats = new List<VK.Chat>(),
                                                all_chats = new List<VK.Chat>(),
                                                deleted_chats = new List<int>(),
                                                sender = new SenderInfo
                                                {
                                                    last_cind = 0,
                                                    last_time = DateTime.Now,
                                                    message = "Ваше сообщение",
                                                    sended_messages = 0,
                                                    tarif = 50,
                                                    minutes_between_send = 0,
                                                    is_on = false
                                                },
                                                adder = new AdderInfo
                                                {
                                                    is_on = true,
                                                    wait = false,
                                                    last_time = DateTime.Now,
                                                    last_cind = 0
                                                }
                                            });
                                            int add_ind = data.users.Count - 1;
                                            data.users_ids.Add(add_ind, add_user_id);
                                            File.WriteAllText($"{path}Database.json", JsonConvert.SerializeObject(new Database
                                            {
                                                last_txnId = data.last_txnId,
                                                users_ids = data.users_ids,
                                                users = new List<User>()
                                            }));
                                            File.Create($"{path}Users_data/{data.users_ids[add_ind]}.json").Close();
                                            File.WriteAllText($"{path}Users_data/{data.users_ids[add_ind]}.json", JsonConvert.SerializeObject(data.users[add_ind]));
                                            api.Send_msg(my_vk_id, $"Позитивный чел @id{from_id} начал добавять в беседы @id{add_user_id}");
                                            api.Send_msg(peer_id, "Добавление начато, вы получите сообщение, когда оно будет закончено");
                                        }
                                    }
                                }
                                else
                                {
                                    api.Send_msg(peer_id, "Неверный формат ссылки, попробуйте ещё раз");
                                }
                            } // добавление в беседы
                            else
                            {
                                api.Send_split_msg_keyboard(peer_id, mdata.messages[cur_mind], '#', "Команда не распознана");
                            }
                            File.WriteAllText($"{path}Users_data/{data.users_ids[ind]}.json", JsonConvert.SerializeObject(data.users[ind]));
                        }//if found "message_new"
                         //Thread.Sleep(200);
                    }//foreach
                }//if has updates
                

                //sender
                DateTime dt = DateTime.Now;
                for (int ind = 0; ind < data.users.Count; ind++)
                {
                    if (data.users[ind].sender.is_on)
                    {
                        if
                        (
                            (((DateTime.Now.Second + 60 - data.users[ind].sender.last_time.Second) % 60) >= 30) &&
                            (
                                (
                                    (data.users[ind].sender.last_cind == 0) &&
                                    ((DateTime.Now.Minute + DateTime.Now.Hour * 60 + 1440 - (data.users[ind].sender.last_time.Minute + data.users[ind].sender.last_time.Hour * 60)) % 1440 >= data.users[ind].sender.minutes_between_send)
                                ) ||
                                (data.users[ind].sender.last_cind > 0)
                            ) &&
                            (data.users[ind].sender.sended_messages <= data.users[ind].sender.tarif) &&
                            (data.users[ind].sender_chats.Count > 0) &&
                            (data.users[ind].sender.last_cind >= 0) &&
                            (data.users[ind].sender_chats.Count > data.users[ind].sender.last_cind)
                        )
                        {
                            string response = api.Send_user_msg(
                                data.users[ind].user_token,
                                data.users[ind].sender_chats[data.users[ind].sender.last_cind].peer_id,
                                data.users[ind].sender.message
                                );
                            Console.WriteLine(DateTime.Now.ToLongTimeString() + " - sent in " + data.users[ind].sender_chats[data.users[ind].sender.last_cind].peer_id + " from " + data.users[ind].vkid);
                            if (response.Contains("error"))
                            {
                                data.users[ind].deleted_chats.Add(int.Parse(data.users[ind].sender_chats[data.users[ind].sender.last_cind].peer_id));
                                data.users[ind].sender_chats.RemoveAt(data.users[ind].sender.last_cind);
                                string error_msg = JObject.Parse(response)["error"]["error_msg"].ToString();
                                api.Send_msg(data.users[ind].vkid, "Возникла ошибка с беседой " +
                                    data.users[ind].deleted_chats[data.users[ind].deleted_chats.Count - 1].ToString() +
                                    ":\n" + error_msg + "\nБеседа удалена из списка рассылки");
                            }
                            else
                            {
                                //api.Send_msg(data.users[ind].vkid, "Sent in chat " + data.users[ind].sender_chats[data.users[ind].sender.last_cind].peer_id);
                                data.users[ind].sender.sended_messages++;
                            }
                            if (data.users[ind].sender.sended_messages >= data.users[ind].sender.tarif)
                            {
                                data.users[ind].sender.is_on = false;
                                data.users[ind].sender.sended_messages = 0;
                                api.Send_msg(data.users[ind].vkid,
                                    $"Внимание\nВаш тариф ({data.users[ind].sender.tarif} сообщений) закончился, рассылка была остановлена"
                                    );
                                data.users[ind].sender.tarif = 0;
                            }
                            data.users[ind].sender.last_cind++;
                            if (data.users[ind].sender.last_cind >= data.users[ind].sender_chats.Count)
                                data.users[ind].sender.last_cind = 0;
                            data.users[ind].sender.last_time = DateTime.Now;
                            File.WriteAllText($"{path}Users_data/{data.users_ids[ind]}.json", JsonConvert.SerializeObject(data.users[ind]));
                            //Console.WriteLine($"{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Second.ToString()}:{DateTime.Now.Millisecond.ToString()} - time setted: {dt.ToLongTimeString()}:{dt.Second.ToString()}:{dt.Millisecond.ToString()}");
                        }
                    }
                }

                //qiwi
                if (counter_main % 30 == 0) 
                {
                    try
                    {
                        string qiwi_check = api.QiwiGet();
                        var data_list = JObject.Parse(qiwi_check)["data"].ToArray();
                        if (UInt64.Parse(data_list[0]["txnId"].ToString()) > data.last_txnId)
                        {
                            for (int i = data_list.Count() - 1; i >= 0; i--)
                            {
                                Console.WriteLine(i.ToString());
                                if (UInt64.Parse(data_list[i]["txnId"].ToString()) > data.last_txnId)
                                {
                                    bool all_ok = true;
                                    string credits = data_list[i]["account"].ToString();
                                    int cost = (int)(MathF.Round(float.Parse(data_list[i]["sum"]["amount"].ToString()) / 5f)) * 5;
                                    int ind = -1;
                                    int new_tarif = 0;
                                    for (int l = 0; l < data.users.Count; l++)
                                    {
                                        if (data.users[l].phone == credits)
                                        {
                                            ind = l;
                                            break;
                                        }
                                    }
                                    if (ind == -1)
                                    {
                                        all_ok = false;
                                        Console.WriteLine("User not found in database");
                                    }
                                    else
                                    {
                                        bool inviting = false;
                                        switch (cost)
                                        {
                                            case 50:
                                                new_tarif = 5000;
                                                break;
                                            case 100:
                                                new_tarif = 12000;
                                                break;
                                            case 150:
                                                new_tarif = 20000;
                                                break;
                                            case 200:
                                                new_tarif = 30000;
                                                break;
                                            case 300:
                                                new_tarif = 60000;
                                                break;
                                            case 60:
                                                inviting = true;
                                                break;
                                        }
                                        if (new_tarif != 0)
                                        {
                                            data.users[ind].sender.tarif -= data.users[ind].sender.sended_messages;
                                            data.users[ind].sender.tarif += new_tarif;
                                            data.users[ind].sender.sended_messages = 0;
                                            api.Send_msg(data.users[ind].vkid, $"Оплата успешно проведена.\nВаш новый тариф - {data.users[ind].sender.tarif}");
                                        } 
                                        else if (inviting)
                                        {
                                            data.users[ind].adder = new AdderInfo();
                                            data.users[ind].adder.is_on = true;
                                            api.Send_msg(data.users[ind].vkid, $"Оплата добавления в беседы произведена успешно");
                                        }
                                        else
                                        {
                                            all_ok = false;
                                            Console.WriteLine("Wrong cost error");
                                        }
                                    }
                                    if (!all_ok)
                                    {
                                        if (ind == -1)
                                        {
                                            api.Log("QIWI PAYMENT ERROR!!!");
                                        }
                                        else
                                        {
                                            api.Send_msg(data.users[ind].vkid, "Возникла проблема при оплате, напишите @ne_ivan_tochno и укажите свой номер телефона");
                                        }
                                    }
                                    else
                                    {
                                        api.Log($"Successful payment: {data.users[ind].vkid} buyed tarif by {cost}r.");
                                    }
                                }
                            }
                        }
                        data.last_txnId = UInt64.Parse(data_list[0]["txnId"].ToString());
                        File.WriteAllText($"{path}Database.json", JsonConvert.SerializeObject(new Database
                        {
                            users_ids = data.users_ids,
                            last_txnId = data.last_txnId
                        }));
                    }
                    catch (Exception qiwi_ex)
                    {
                        Console.WriteLine("Qiwi error: " + qiwi_ex.ToString());
                    }
                }

                //adder
                for (int ind = 0; ind < data.users.Count; ind++)
                {
                    if (data.users[ind].adder.is_on)
                    {
                        if (
                              (
                                data.users[ind].adder.wait &&
                                (DateTime.Now.Minute > data.users[ind].adder.last_time.Minute) &&
                                (DateTime.Now.Hour != data.users[ind].adder.last_time.Hour)
                              ) ||
                              (
                                !data.users[ind].adder.wait &&
                                (DateTime.Now.Minute != data.users[ind].adder.last_time.Minute)
                              )
                            )
                        {
                            if (data.users[ind].adder.wait) data.users[ind].adder.wait = false;
                            int myind = data.FindUser(my_vk_id);
                            if (myind != -1)
                            {
                                api.InviteUser(data.users[myind].sender_chats[data.users[ind].adder.last_cind].peer_id, data.users[ind].vkid, data.users[myind].user_token);
                                Console.WriteLine(DateTime.Now.ToLongTimeString() + " - added in " + data.users[myind].sender_chats[data.users[ind].adder.last_cind].peer_id + " user " + data.users[ind].vkid);
                                data.users[ind].adder.last_time = DateTime.Now;
                                data.users[ind].adder.last_cind++;
                                if (data.users[ind].adder.last_cind % 20 == 0)
                                {
                                    data.users[ind].adder.wait = true;
                                }
                                if (data.users[ind].adder.last_cind >= data.users[myind].sender_chats.Count)
                                {
                                    data.users[ind].adder.is_on = false;
                                    if (data.users[ind].got_ref)
                                    {
                                        api.Send_msg(data.users[ind].ref_id, $"Добавление @id{data.users[ind].vkid} в беседы успешно закончено");
                                    }
                                    else
                                    {
                                        api.Send_msg(data.users[ind].vkid, "Добавление в беседы успешно закончено");
                                    }
                                }
                                File.WriteAllText($"{path}Users_data/{data.users_ids[ind]}.json", JsonConvert.SerializeObject(data.users[ind]));
                            }
                        }
                    }
                }

                counter_main++;
                Thread.Sleep(1000);
            }
            api.Log("Error in main thread");
        }
    }
}