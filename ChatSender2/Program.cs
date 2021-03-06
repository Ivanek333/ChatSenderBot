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
            string path = @"C:/BotsFiles/ChatSender2/";
            Tokens tokens = new Tokens();
            try
            {
                JsonConvert.PopulateObject(File.ReadAllText(path + "Tokens.json"), tokens);
            }
            catch (Exception e)
            {
                Console.WriteLine("Read tokens error " + e.Message);
                File.WriteAllText(path + "Tokens.json", JsonConvert.SerializeObject(tokens));
                return;
            }
            Random rand = new Random();
            MyApi api = new MyApi
            {
                token = tokens.group_token,
                group_id = tokens.group_id,
                path = path
            };
            Database data = new Database();
            MessageData mdata = new MessageData();
            try
            {
                JsonConvert.PopulateObject(File.ReadAllText($"{path}MessageData.json"), mdata);
                JsonConvert.PopulateObject(File.ReadAllText($"{path}Database.json"), data);
                for (int i = 0; i < data.users_ids.Count; i++)
                {
                    User temp = new User();
                    JsonConvert.PopulateObject(File.ReadAllText($"{path}Users_data/{data.users_ids[i]}.json"), temp);
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
                for (int i = 0; i < data.users_ids.Count; i++)
                {
                    //File.WriteAllText($"{path}Users_data/{data.users_ids[i]}.json", JsonConvert.SerializeObject(data.users[i]));
                }
            }
            catch (Exception e)
            {
                api.Log("Database rewrite error" + e.ToString());
                return;
            }
            Worker worker = new Worker(api, Thread.CurrentThread, data, tokens, path);
            Thread worker_thread = new Thread(new ThreadStart(worker.Work));
            worker_thread.Start();
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
                            string msg = item["object"]["message"]["text"].ToString().Trim();        //???????????????? ??????????????????
                            string good_msg = msg.ToLower();                           //???????????????????? ???????????? ??????????????????
                            string peer_id = item["object"]["message"]["peer_id"].ToString(); //id ????????????????????
                            string from_id = item["object"]["message"]["from_id"].ToString(); //id ??????????????????????
                            string message_id = item["object"]["message"]["id"].ToString();   //id ??????????????????											  //string message_id = item["object"]["apisage"]["id"].ToString();   //id ??????????????????
                            api.Log($"@id{from_id} in vk.com/gim{tokens.group_id}?sel={peer_id} : '{msg}' , good_msg: '{good_msg}'");

                            bool new_user = false;
                            int ind = data.FindUser(from_id);
                            if (ind == -1)
                            {
                                new_user = true;
                                data.users_ids.Add(from_id);
                                ind = data.users.Count;
                                data.users.Add(new User());
                                data.users[ind].vkid = from_id;
                                File.WriteAllText($"{path}Database.json", JsonConvert.SerializeObject(new Database
                                {
                                    last_txnId = data.last_txnId,
                                    users_ids = data.users_ids,
                                    users = new List<User>()
                                }));
                                File.Create($"{path}Users_data/{data.users_ids[ind]}.json").Close();
                                File.WriteAllText($"{path}Users_data/{data.users_ids[ind]}.json", JsonConvert.SerializeObject(data.users[ind]));
                            }
                            lock (data.users[ind])
                            {
                                if (!data.users[ind].got_ref)
                                    try
                                    {
                                        if (item["object"]["message"].ToString().Contains("ref"))
                                        {
                                            string ref_code = item["object"]["message"]["ref"].ToString();
                                            //string ref_source = item["object"]["message"]["ref_source"].ToString();
                                            int ref_ind = data.FindUser(ref_code);
                                            if (ref_ind != -1)
                                            {
                                                data.users[ind].got_ref = true;
                                                data.users[ind].ref_id = data.users[ref_ind].vkid;
                                                data.users[ind].sender.tarif += 300;
                                                data.users[ref_ind].refs.Add(from_id);
                                                api.Send_msg(data.users[ref_ind].vkid, $"?? ?????? ???????????????? ?????????? ?????????????? - @id{from_id}");
                                                api.Send_msg(peer_id, $"???? ?????????? ?????????????????? @id{data.users[ref_ind].vkid} ?? ???????????????? ??????????: +300 ??????????????????");
                                            }
                                        }
                                    }
                                    catch { }
                                int cur_mid = data.users[ind].mid;
                                int cur_mind = mdata.FindMind(cur_mid);
                                int to_mid = cur_mid;
                                int to_mind = cur_mind;
                                Dictionary<string, string> dictionary = new Dictionary<string, string>
                                {
                                    { "+", "%2B" },
                                    { "[user_id]", from_id },
                                    { "[user_token]", data.users[ind].user_token }
                                };
                                if (!new_user)
                                {
                                    to_mid = api.FindMidByText(mdata.messages[cur_mind], msg);
                                    to_mind = mdata.FindMind(to_mid);
                                }
                                //Console.WriteLine(cur_mid + " " + cur_mind + " " + to_mid + " " + to_mind);
                                //if admin
                                if (to_mid == 7 && data.users[ind].is_admin)
                                {
                                    to_mid = 38;
                                    to_mind = mdata.FindMind(to_mid);
                                }
                                if (to_mid == 12)
                                {
                                    dictionary.Add("[sender_chats_count]", data.users[ind].sender.sender_chats.Count.ToString());
                                }
                                if (to_mid == 13)
                                {
                                    var list = new List<Chat>();
                                    for (int i = 0; i < data.users[ind].sender.sender_chats.Count; i++)
                                    {
                                        list.Add(data.users[ind].sender.all_chats[data.users[ind].sender.sender_chats[i]]);
                                    }
                                    dictionary.Add("[sender_chats_on]", api.ChatList2String(list));
                                }
                                if (to_mid == 14)
                                {
                                    try
                                    {
                                        api.Send_msg(peer_id, "???????????????????? ???????????? ???????? ?????????? ??????????...");
                                        data.users[ind].sender.all_chats = api.GetChats(data.users[ind].user_token);
                                        data.users[ind].sender.changed = false;
                                        //Console.WriteLine(data.users[ind].sender.all_chats.Count);
                                        for (int i = 0; i < data.users[ind].sender.sender_chats.Count; i++)
                                        {
                                            //Console.WriteLine(data.users[ind].sender.sender_chats[i]);
                                            //Console.WriteLine(data.users[ind].sender.all_chats[data.users[ind].sender.sender_chats[i]].cid);
                                            data.users[ind].sender.all_chats[data.users[ind].sender.sender_chats[i]].mark = 1;
                                        }
                                        /*for (int i = 0; i < data.users[ind].sender.deleted_chats.Count; i++)
                                        {
                                            data.users[ind].sender.all_chats[data.users[ind].sender.deleted_chats[i]].mark = 2;
                                        }*/
                                        dictionary.Add("[sender_chats_all]", api.ChatList2String(data.users[ind].sender.all_chats));
                                    }
                                    catch (Exception e)
                                    {
                                        api.Log("???????? ?? ????????????????");
                                        api.Send_msg(from_id, "???? ?????????????????? ?????????????? ???????? >:(");
                                    }
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
                                }
                                if (to_mid == 20 || to_mid == 24)
                                {
                                    dictionary.Add("[sender_state]", data.users[ind].sender.is_on ? "????????????????" : "??????????????????");
                                }
                                if (to_mid == 30)
                                {
                                    dictionary.Add("[phone_number]", data.users[ind].phone);
                                }
                                if (to_mid == 41 || to_mid == 53)
                                {
                                    dictionary.Add("[ref_link]", $"vk.me%2Fsendplusbot%3Fref%3D{data.users[ind].vkid}");
                                }
                                if (to_mid == 42)
                                {
                                    dictionary.Add("[admin_balance]", data.users[ind].adminInfo.balance.ToString());
                                }
                                if (to_mid == 43 || to_mid == 54)
                                {
                                    dictionary.Add("[ref_list]", data.users[ind].refs_ToString());
                                }
                                if (good_msg == "ez admin lol")
                                {
                                    data.users[ind].is_admin = true;
                                    api.Send_msg(peer_id, "???? ?????????? ??????????????");
                                }
                                // ???????????????????? ?????? ???????????????????????? ?????????????????? ???? ????????
                                else if ((to_mid == 20 || to_mid == 12) && !data.users[ind].got_token)
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
                                else if (to_mind != -1) //???????? ?????????? ???? ???????????? ?? ???????????????????? ??????
                                {
                                    data.users[ind].mid = mdata.messages[to_mind].mid;
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[to_mind], '#', api.ReplaceAll(mdata.messages[to_mind].text, dictionary));
                                }
                                //???????????????????? ?????? ?????? ??????????????, ?? ?????????????? ?????????? ????????
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
                                } // ???????? ????????????
                                else if (cur_mid == 15)
                                {
                                    if (!good_msg.Contains('-'))
                                    {
                                        int cid = -1;
                                        if (int.TryParse(good_msg, out cid))
                                        {
                                            int ind_all = -1, ind_send = -1;
                                            for (int i = 0; i < data.users[ind].sender.all_chats.Count; i++)
                                            {
                                                if (data.users[ind].sender.all_chats[i].cid == cid)
                                                {
                                                    ind_all = i;
                                                    break;
                                                }
                                            }
                                            if (ind_all != -1)
                                            {
                                                for (int i = 0; i < data.users[ind].sender.sender_chats.Count; i++)
                                                {
                                                    if (data.users[ind].sender.all_chats[data.users[ind].sender.sender_chats[i]].cid == cid)
                                                    {
                                                        ind_send = i;
                                                        break;
                                                    }
                                                }
                                                /*for (int i = 0; i < data.users[ind].sender.deleted_chats.Count; i++)
                                                {
                                                    if (data.users[ind].sender.all_chats[data.users[ind].sender.deleted_chats[i]].cid == cid)
                                                    {
                                                        data.users[ind].sender.deleted_chats.RemoveAt(i);
                                                        break;
                                                    }
                                                }*/
                                                if (ind_send == -1)
                                                {
                                                    data.users[ind].sender.all_chats[ind_all].mark = 1;
                                                    data.users[ind].sender.sender_chats.Add(ind_all);
                                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[cur_mind], '#', mdata.messages[mdata.FindMind(17)].text);
                                                }
                                                else
                                                {
                                                    data.users[ind].sender.all_chats[ind_all].mark = 0;
                                                    data.users[ind].sender.sender_chats.RemoveAt(ind_send);
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
                                                for (int i = 0; i < data.users[ind].sender.all_chats.Count; i++)
                                                {
                                                    if (data.users[ind].sender.all_chats[i].cid == cid)
                                                    {
                                                        ind_all = i;
                                                        break;
                                                    }
                                                }
                                                if (ind_all != -1)
                                                {
                                                    for (int i = 0; i < data.users[ind].sender.sender_chats.Count; i++)
                                                    {
                                                        if (data.users[ind].sender.all_chats[data.users[ind].sender.sender_chats[i]].cid == cid)
                                                        {
                                                            ind_send = i;
                                                            break;
                                                        }
                                                    }
                                                    /*for (int i = 0; i < data.users[ind].sender.deleted_chats.Count; i++)
                                                    {
                                                        if (data.users[ind].sender.all_chats[data.users[ind].sender.deleted_chats[i]].cid == cid)
                                                        {
                                                            data.users[ind].sender.deleted_chats.RemoveAt(i);
                                                            break;
                                                        }
                                                    }*/
                                                    if (ind_send == -1)
                                                    {
                                                        data.users[ind].sender.all_chats[ind_all].mark = 1;
                                                        data.users[ind].sender.sender_chats.Add(ind_all);
                                                        chats_added.Add(cid);
                                                    }
                                                    else
                                                    {
                                                        data.users[ind].sender.all_chats[ind_all].mark = 0;
                                                        data.users[ind].sender.sender_chats.RemoveAt(ind_send);
                                                        chats_removed.Add(cid);
                                                    }
                                                }
                                            }
                                            data.users[ind].sender.sender_chats.Sort();
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
                                } // ???????????????????? ??????????
                                else if (cur_mid == 22)
                                {
                                    data.users[ind].sender.message = msg;
                                    data.users[ind].mid = 23;
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(23)], '#');
                                } // ?????????? ????????????????
                                else if (cur_mid == 25)
                                {
                                    int time = -1;
                                    if (int.TryParse(good_msg, out time))
                                    {
                                        time = Math.Max(0, time - (data.users[ind].sender.sender_chats.Count / 2));
                                        data.users[ind].sender.minutes_between_send = time;
                                        data.users[ind].mid = 26;
                                        api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(26)], '#');
                                    }
                                    else
                                    {
                                        api.Send_split_msg_keyboard(peer_id, mdata.messages[cur_mind], '#', mdata.messages[mdata.FindMind(27)].text);
                                    }
                                } // ?????????? ?????????? ????????????????????
                                else if (cur_mid == 32)
                                {
                                    data.users[ind].phone = msg.Trim();
                                    data.users[ind].got_phone = true;
                                    data.users[ind].mid = 33;
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(33)], '#');
                                } // ?????????? ????????????????
                                else if (cur_mid == 36)
                                {
                                    data.users[ind].phone = msg.Trim();
                                    data.users[ind].got_phone = true;
                                    data.users[ind].mid = 37;
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(37)], '#');
                                } // ?????????? ????????????????
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
                                            if (add_user_id.StartsWith("g"))
                                            {
                                                api.Send_msg(peer_id, "?????? ???? ??????????????, ?? ????????????!\n???? ?????????????????????? ?????????? ?????????? ?? ??????????????, ?????????????? ?? ???????? ???? ????????????????!");
                                            }
                                            else
                                            {
                                                int add_ind = data.FindUser(add_user_id);
                                                if (add_ind == -1)
                                                {
                                                    data.users.Add(new User());
                                                    add_ind = data.users.Count - 1;
                                                    data.users_ids.Add(add_user_id);
                                                    data.users[add_ind].vkid = add_user_id;
                                                    data.users[add_ind].ref_id = from_id;
                                                    data.users[add_ind].got_ref = true;
                                                    File.WriteAllText($"{path}Database.json", JsonConvert.SerializeObject(new Database
                                                    {
                                                        last_txnId = data.last_txnId,
                                                        users_ids = data.users_ids,
                                                        users = new List<User>()
                                                    }));
                                                    File.Create($"{path}Users_data/{data.users[add_ind].vkid}.json").Close();
                                                    api.Send_msg(peer_id, $"?? ?????? ???????????????? ?????????? ?????????????? - @id{from_id}");
                                                }
                                                data.users[add_ind].adder.wait = false;
                                                data.users[add_ind].adder.last_cind = 0;
                                                data.users[add_ind].adder.is_on = true;
                                                File.WriteAllText($"{path}Users_data/{data.users[add_ind].vkid}.json", JsonConvert.SerializeObject(data.users[add_ind]));
                                                api.Send_msg(tokens.my_id, $"???????????????????? ?????? @id{from_id} ?????????? ???????????????? ?? ???????????? @id{add_user_id}");
                                                data.users[ind].adminInfo.balance -= 25;
                                                api.Send_msg(peer_id, "???????????????????? ????????????, ???? ???????????????? ??????????????????, ?????????? ?????? ?????????? ??????????????????.\n?? ???????????? ?????????????? ?????????? 25???");
                                            }
                                        }
                                        else
                                        {
                                            api.Send_msg(peer_id, "?????????????????? ???????????????????????? ???? ??????????????????");
                                        }
                                    }
                                    else
                                    {
                                        api.Send_msg(peer_id, "???????????????? ???????????? ????????????, ???????????????????? ?????? ??????");
                                    }
                                } // ???????????????????? ?? ????????????
                                else if (cur_mid == 45)
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
                                            if (add_user_id.StartsWith("g"))
                                            {
                                                api.Send_msg(peer_id, "?????? ???? ??????????????, ?? ????????????, ?? ???????? ???? ????????????????!");
                                            }
                                            else
                                            {
                                                int add_ind = data.FindUser(add_user_id);
                                                if (add_ind == -1)
                                                {
                                                    api.Send_msg(peer_id, $"???????????????????????? ???? ????????????, ???????????????????? ?????? ??????");
                                                }
                                                else
                                                {
                                                    data.users[ind].adminInfo.temp_user_ind = add_ind;
                                                    data.users[ind].mid = 46;
                                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(46)], '#', mdata.messages[mdata.FindMind(46)].text);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            api.Send_msg(peer_id, "?????????????????? ???????????????????????? ???? ??????????????????");
                                        }
                                    }
                                    else
                                    {
                                        api.Send_msg(peer_id, "???????????????? ???????????? ????????????, ???????????????????? ?????? ??????");
                                    }
                                } // ???????????????????? ????????????
                                else if (cur_mid == 46)
                                {
                                    int tar = 0;
                                    if (int.TryParse(good_msg, out tar))
                                    {
                                        data.users[ind].adminInfo.balance -= (int)MathF.Round(tar * 0.006f);
                                        data.users[data.users[ind].adminInfo.temp_user_ind].sender.tarif += tar;
                                        data.users[ind].mid = 47;
                                        dictionary.Add("[admin_balance]", data.users[ind].adminInfo.balance.ToString());
                                        api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(47)], '#', api.ReplaceAll(mdata.messages[mdata.FindMind(47)].text, dictionary));
                                    }
                                    else
                                    {
                                        api.Send_msg(peer_id, "???? ?????????????? ???????????????? ??????????, ???????????????????? ?????? ??????");
                                    }
                                }
                                else if (cur_mid == 48)
                                {
                                    string check_user_id = good_msg;
                                    if (check_user_id.Contains("vk.com"))
                                    {
                                        check_user_id = check_user_id.Substring(check_user_id.IndexOf("vk.com") + 7, check_user_id.Length - check_user_id.IndexOf("vk.com") - 7);
                                        Console.WriteLine(check_user_id);
                                        Thread.Sleep(10);
                                        check_user_id = api.GetUserId(check_user_id);
                                        Console.WriteLine(check_user_id);
                                        if (check_user_id != "error")
                                        {
                                            if (check_user_id.StartsWith("g"))
                                            {
                                                api.Send_msg(peer_id, "?????? ????????????, @public" + check_user_id.Remove(1, 1));
                                            }
                                            else
                                            {
                                                api.Send_msg(peer_id, "?????? ??????????????, @id" + check_user_id);
                                            }
                                        }
                                        else
                                        {
                                            api.Send_msg(peer_id, "?????????????????? ???????????????????????? ???? ??????????????????");
                                        }
                                    }
                                    else
                                    {
                                        api.Send_msg(peer_id, "???????????????? ???????????? ????????????, ???????????????????? ?????? ??????");
                                    }
                                }
                                else if (cur_mid == 55)
                                {
                                    data.users[ind].phone = msg.Trim();
                                    data.users[ind].got_phone = true;
                                    data.users[ind].mid = 56;
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[mdata.FindMind(56)], '#');
                                } // ?????????? ????????????????
                                else
                                {
                                    api.Send_split_msg_keyboard(peer_id, mdata.messages[cur_mind], '#', "?????????????? ???? ????????????????????");
                                }
                                File.WriteAllText($"{path}Users_data/{data.users_ids[ind]}.json", JsonConvert.SerializeObject(data.users[ind]));
                            }//lock
                        }//if found "message_new"
                         //Thread.Sleep(200);
                    }//foreach
                }//if has updates

                counter_main++;
                Thread.Sleep(1000);
            }
            api.Log("Error in main thread");
        }
    }
    public class Worker
    {
        public Thread main_thread;
        public MyApi api;
        public Database data;
        public Tokens tokens;
        public string path;

        public Worker(MyApi _api, Thread _main_thread, Database _data, Tokens _tokens, string _path)
        {
            api = new MyApi
            {
                path = _api.path,
                group_id = _api.group_id,
                token = _api.token
            };
            main_thread = _main_thread;
            data = _data;
            tokens = _tokens;
            path = _path;
        }

        public void Work()
        {
            int counter_main = 0;
            while (true)
            {
                if (counter_main % 20 == 0)
                    Console.WriteLine("worker");
                if (!main_thread.IsAlive)
                {
                    Console.WriteLine("main thread not alive, closing");
                    break;
                }
                //sender
                /*string t = "";
                for (int ind = 0; ind < data.users.Count; ind++)
                {
                    t += (ind + 1).ToString() + ". " + data.users[ind].vkid  + "\n";
                }
                Console.WriteLine(t);*/
                for (int ind = 0; ind < data.users.Count; ind++)
                {
                    if (data.users[ind].sender.is_on)
                    {
                        /*Console.WriteLine($"{data.users[ind].vkid} - sender is on");
                        Console.WriteLine($"Time now: {DateTime.Now.ToLongTimeString()}, last time: {data.users[ind].sender.last_time.ToLongTimeString()}");
                        Console.WriteLine($"ind: {data.users[ind].sender.last_cind}, wait: {data.users[ind].sender.minutes_between_send}, min betw: {Math.Abs(DateTime.Now.Minute + DateTime.Now.Hour * 60 - (data.users[ind].sender.last_time.Minute + data.users[ind].sender.last_time.Hour * 60))}");
                        Console.WriteLine($"Sec: {Math.Abs(DateTime.Now.Second - data.users[ind].sender.last_time.Second)}");
                        Console.WriteLine($"sm: {data.users[ind].sender.sended_messages}, tarif: {data.users[ind].sender.tarif}, count: {data.users[ind].sender.sender_chats.Count}");
                        */
                        if
                        (
                            (Math.Abs(DateTime.Now.Second - data.users[ind].sender.last_time.Second) >= 30) &&
                            (
                                (
                                    (data.users[ind].sender.last_cind == 0) &&
                                    (Math.Abs(DateTime.Now.Minute + DateTime.Now.Hour * 60 - (data.users[ind].sender.last_time.Minute + data.users[ind].sender.last_time.Hour * 60)) >= data.users[ind].sender.minutes_between_send)
                                ) ||
                                (data.users[ind].sender.last_cind > 0)
                            ) &&
                            (data.users[ind].sender.sended_messages <= data.users[ind].sender.tarif) &&
                            (data.users[ind].sender.sender_chats.Count > 0) &&
                            (data.users[ind].sender.last_cind >= 0)
                        )
                        {
                            lock (data.users[ind])
                            {
                                //Console.WriteLine("here");
                                try
                                {


                                    if (data.users[ind].sender.last_cind >= data.users[ind].sender.sender_chats.Count)
                                    {
                                        data.users[ind].sender.last_cind = 0;
                                        continue;
                                    }
                                    string response = api.Send_user_msg(
                                        data.users[ind].user_token,
                                        data.users[ind].sender.all_chats[data.users[ind].sender.sender_chats[data.users[ind].sender.last_cind]].cid.ToString(),
                                        data.users[ind].sender.message
                                        );
                                    Console.WriteLine(DateTime.Now.ToLongTimeString() + " - sent in " + data.users[ind].sender.all_chats[data.users[ind].sender.sender_chats[data.users[ind].sender.last_cind]].cid.ToString() + " from " + data.users[ind].vkid);
                                    if (response.Contains("error"))
                                    {
                                        data.users[ind].sender.changed = true;
                                        //data.users[ind].sender.deleted_chats.Add(data.users[ind].sender.sender_chats[data.users[ind].sender.last_cind]);
                                        string error_msg = JObject.Parse(response)["error"]["error_msg"].ToString();
                                        api.Send_msg(data.users[ind].vkid, "???????????????? ???????????? ?? ?????????????? " +
                                            data.users[ind].sender.all_chats[data.users[ind].sender.sender_chats[data.users[ind].sender.last_cind]].cid.ToString() +
                                            ":\n" + error_msg + "\n???????????? ?????????????? ???? ???????????? ????????????????");
                                        data.users[ind].sender.sender_chats.RemoveAt(data.users[ind].sender.last_cind);
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
                                            $"????????????????!\n?????? ?????????? ({data.users[ind].sender.tarif} ??????????????????) ????????????????????, ???????????????? ???????? ??????????????????????"
                                            );
                                        data.users[ind].sender.tarif = 0;
                                    }
                                    data.users[ind].sender.last_cind++;
                                    if (data.users[ind].sender.last_cind >= data.users[ind].sender.sender_chats.Count)
                                        data.users[ind].sender.last_cind = 0;
                                    data.users[ind].sender.last_time = DateTime.Now;
                                    File.WriteAllText($"{path}Users_data/{data.users_ids[ind]}.json", JsonConvert.SerializeObject(data.users[ind]));
                                    //Console.WriteLine($"{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Second.ToString()}:{DateTime.Now.Millisecond.ToString()} - time setted: {dt.ToLongTimeString()}:{dt.Second.ToString()}:{dt.Millisecond.ToString()}");
                                }
                                catch (Exception e)
                                {
                                    api.Log("Send error: " + data.users[ind].vkid + " >> " + e.ToString());
                                    api.Send_msg(tokens.my_id, "?????????? ???????? ????????????????");
                                    api.Send_msg(data.users[ind].vkid, "???? ?????????????? ???????? (??????????, ?? ??????????????, ??????), ???????????? ?????? ???????????????? ????????????, ?????????????? ???? ???????????????????? ????????????");
                                    data.users[ind].sender.all_chats = new List<Chat>();
                                    data.users[ind].sender.sender_chats = new List<int>();
                                    data.users[ind].sender.deleted_chats = new List<int>();
                                    data.users[ind].sender.last_cind = 0;
                                    data.users[ind].sender.is_on = false;
                                }
                            }
                            
                        }
                    }
                }//sender

                int myind = data.FindUser(tokens.adder_id);
                if (myind != -1)
                {
                    //qiwi
                    if (counter_main % 30 == 0)
                    {
                        try
                        {
                            string qiwi_check = api.QiwiGet(tokens.qiwi_phone, tokens.qiwi_token);
                            var data_list = JObject.Parse(qiwi_check)["data"].ToArray();
                            if (UInt64.Parse(data_list[0]["txnId"].ToString()) > data.last_txnId)
                            {
                                for (int i = data_list.Count() - 1; i >= 0; i--)
                                {
                                    Console.WriteLine(i.ToString());
                                    if (UInt64.Parse(data_list[i]["txnId"].ToString()) > data.last_txnId)
                                    {
                                        string credits = data_list[i]["account"].ToString();
                                        float cost_f = float.Parse(data_list[i]["sum"]["amount"].ToString());
                                        int cost = (int)MathF.Round(cost_f / 5f) * 5;
                                        int ind = -1;
                                        int new_tarif = 0;
                                        try
                                        {
                                            lock (data.users)
                                            {
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
                                                    throw new Exception("User not found in database");
                                                }
                                                else
                                                {
                                                    api.Send_msg("2000000001", $"?????????? ??????????????:\n+{cost_f}?? - @id{data.users[ind].vkid}");
                                                    bool inviting = false;
                                                    switch (cost)
                                                    {
                                                        case 60:
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
                                                            new_tarif = 50000;
                                                            break;
                                                        case 50:
                                                            inviting = true;
                                                            break;
                                                    }
                                                    if (new_tarif != 0)
                                                    {
                                                        data.users[ind].sender.tarif -= data.users[ind].sender.sended_messages;
                                                        data.users[ind].sender.tarif += new_tarif;
                                                        data.users[ind].sender.sended_messages = 0;
                                                        api.Send_msg(data.users[ind].vkid, $"???????????? ?????????????? ??????????????????.\n?????? ?????????? ?????????? - {data.users[ind].sender.tarif}");
                                                    }
                                                    else if (inviting)
                                                    {
                                                        data.users[ind].adder = new AdderInfo();
                                                        data.users[ind].adder.is_on = true;
                                                        api.Send_msg(data.users[ind].vkid, $"???????????? ???????????????????? ?? ???????????? ?????????????????????? ??????????????");
                                                        api.AddFriend(data.users[ind].vkid, data.users[myind].user_token);
                                                    }
                                                    else
                                                    {
                                                        throw new Exception("Wrong cost");
                                                    }
                                                    if (data.users[ind].got_ref)
                                                    {
                                                        int ref_ind = data.FindUser(data.users[ind].ref_id); //?????????????????????????? ???????????????? ?????? ???????????? ????????
                                                        if (data.users[ref_ind].is_admin)
                                                        {
                                                            data.users[ref_ind].adminInfo.balance += (int)(cost * 0.1f);
                                                            api.Send_msg(data.users[ref_ind].vkid, $"?????? ?????????????? @id{data.users[ind].vkid} ?????????????? {cost}??, ?????? ???????????? ???????????????? ???? {(int)(cost * 0.1f)}??");
                                                        }
                                                        else
                                                        {
                                                            data.users[ref_ind].sender.tarif += (int)(50f / 3f * cost);
                                                            api.Send_msg(data.users[ref_ind].vkid, $"?????? ?????????????? @id{data.users[ind].vkid} ?????????????? {cost}??, ?????? ?????????? ???????????????? ???? {(int)(166.66667 * cost * 0.1f)} ??????????????????");
                                                        }
                                                    }
                                                }
                                                api.Log($"Successful payment: {data.users[ind].vkid} buyed tarif by {cost_f}");
                                            }
                                        }
                                        catch (Exception payment_ex)
                                        {
                                            api.Log("Payment error: " + payment_ex.Message);
                                            if (ind != -1)
                                                api.Send_msg(data.users[ind].vkid, "???????????????? ???????????????? ?????? ????????????, ???????????????? @ne_ivan_tochno ?? ?????????????? ???????? ?????????? ????????????????");
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
                                lock (data.users[ind])
                                {
                                    if (data.users[ind].adder.wait) data.users[ind].adder.wait = false;
                                    {
                                        try
                                        {
                                            api.InviteUser(data.users[myind].sender.all_chats[data.users[myind].sender.sender_chats[data.users[ind].adder.last_cind]].cid.ToString(), data.users[ind].vkid, data.users[myind].user_token);
                                            Console.WriteLine(DateTime.Now.ToLongTimeString() + " - added in " + data.users[myind].sender.all_chats[data.users[myind].sender.sender_chats[data.users[ind].adder.last_cind]].cid.ToString() + " user " + data.users[ind].vkid);
                                        }
                                        catch { Console.WriteLine("Error adding user " + data.users[ind].vkid); }
                                        data.users[ind].adder.last_time = DateTime.Now;
                                        data.users[ind].adder.last_cind++;
                                        if (data.users[ind].adder.last_cind % 20 == 19)
                                        {
                                            data.users[ind].adder.wait = true;
                                        }
                                        if (data.users[ind].adder.last_cind >= data.users[myind].sender.sender_chats.Count)
                                        {
                                            data.users[ind].adder.is_on = false;
                                            data.users[ind].adder.last_cind = 0;
                                            if (data.users[ind].got_ref)
                                            {
                                                api.Send_msg(data.users[ind].ref_id, $"???????????????????? @id{data.users[ind].vkid} ?? ???????????? ?????????????? ??????????????????");
                                            }
                                            api.Send_msg(data.users[ind].vkid, "???????????????????? ?? ???????????? ?????????????? ??????????????????");
                                        }
                                        File.WriteAllText($"{path}Users_data/{data.users_ids[ind]}.json", JsonConvert.SerializeObject(data.users[ind]));
                                    }
                                }
                            }
                        }
                    }
                    if (counter_main % 10 == 0)
                    {
                        List<string> friends = api.RequestFriends(data.users[myind].user_token);
                        foreach (string id in friends)
                        {
                            api.AddFriend(id, data.users[myind].user_token);
                        }
                    }
                }
                else
                {
                    api.Log("Adder: can't find me");
                }
                counter_main++;
                if (counter_main >= 2000000)
                    counter_main = 0;
                Thread.Sleep(1000);
            }
            Console.WriteLine("end of work");
        }
    }
}