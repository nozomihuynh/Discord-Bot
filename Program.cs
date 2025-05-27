using Discord_Bot.config;
using DSharpPlus;
using DSharpPlus.CommandsNext;
//using System;
using DSharpPlus.EventArgs;
using DSharpPlus.Net;
using System;
using System.Collections;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Discord_Bot
{
    internal class Program
    {
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; } 
        static async Task Main(string[] args)
        {
            var jsonReader = new JSONReader();
            await jsonReader.ReadJSON();
            
            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            Client = new DiscordClient(discordConfig);
            Client.Ready += Client_Ready;
            Client.MessageCreated += OnMessageCreated;

            await Client.ConnectAsync();
            await Task.Delay(-1);   //Assure bot stay online as long as program is on

            //string connectionString = jsonReader.conn;
            //SqlConnection sqlConnection = new SqlConnection(connectionString);
            //sqlConnection.Open();
            //// do something with DB...
        }

        private static async Task OnMessageCreated(DiscordClient client, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || string.IsNullOrWhiteSpace(e.Message.Content))
                return;

            string content = e.Message.Content.Trim();

            if (content.StartsWith("DH*", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
                string response = "";

                foreach (string code in orderCodes)
                {
                    response += code + "\n";
                }

                await e.Message.RespondAsync(response.Trim());
            }
        }


        private static async Task ShowInstruction(ulong channelId)
        {
            var channel = await Client.GetChannelAsync(channelId);
            if (channel == null) return;

            string message =
                "Bot hỗ trợ các cú pháp sau:\n" +
                "1. Duyệt đơn hàng: `DH*DH-0010-0523`\n" +
                "2. Duyệt báo giá: `BG*BG-0123-0523`\n" +
                "3. Gia hạn báo giá (thêm 2 tháng): `GBG*BG-0123-0523`\n" +
                "4. Hủy duyệt đơn hàng: `HUY*DH-0123-0523`\n" +
                "5. Reset đẩy KHSX: `RES*KH-0123-0523`\n" +
                "6. Xóa LSX trong 1 đơn: `XOA*LSX-01234-0523`\n" +
                "7. Đổi MaKC của lệnh: `KC:MãKC,LSX-01234-0523`";

            await channel.SendMessageAsync(message);
        }
        private static ArrayList GetUniqueOrderCodes(string input)
        {
            ArrayList orderCodes = new ArrayList();
            string[] parts = input.Split(new[] { "*" }, StringSplitOptions.None);

            if (parts.Length > 1)
            {
                string orderCodeString = parts[1];
                string[] codes = orderCodeString.Split(',');

                foreach (string code in codes)
                {
                    string trimmedCode = code.Trim();
                    if (!orderCodes.Contains(trimmedCode))
                    {
                        orderCodes.Add(trimmedCode);
                    }
                }


            }
            else
            {
                string orderCodeString = parts[0];
                string trimmedCode = orderCodeString.Trim();
                if (!orderCodes.Contains(trimmedCode))
                {
                    orderCodes.Add(trimmedCode);
                }
            }

            return orderCodes;
        }


        //private static void ProcessDuyetDHCommand_TanLong(long chatId, string command, string userName)
        //{
        //    //string orderCode = command.Replace("phcc_duyetpo***", "").Trim();
        //    ArrayList orderCodes = GetUniqueOrderCodes(command);
        //    ApproveOrder_TanLong(chatId, orderCodes, userName);
        //}
        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }

    }
}
