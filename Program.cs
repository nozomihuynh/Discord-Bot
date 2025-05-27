using Discord_Bot.config;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

//using System;
using DSharpPlus.EventArgs;
using DSharpPlus.Net;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Contexts;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Discord_Bot
{
    internal class Program
    {
        private static bool UseTest = false;
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
            connSV1 = jsonReader.conn;
            Client = new DiscordClient(discordConfig);
            Client.Ready += Client_Ready;
            Client.MessageCreated += OnMessageCreated;
            Console.WriteLine("connSV1: " + connSV1);

            await Client.ConnectAsync();
            await Task.Delay(-1);   //Assure bot stay online as long as program is on

        }
        public static string connSV1;
        private static async Task OnMessageCreated(DiscordClient client, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || string.IsNullOrWhiteSpace(e.Message.Content))
                return;

            string content = e.Message.Content.Trim();
            var member = await e.Guild.GetMemberAsync(e.Author.Id);
            string displayName = string.IsNullOrWhiteSpace(member.Nickname) ? member.Username : member.Nickname;
            string UserName = $"{displayName}#{e.Author.Discriminator}";

            if (content.StartsWith("/start", StringComparison.OrdinalIgnoreCase) || content.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
            {
                await ShowInstruction(e.Channel.Id);
            }
            else if (content.StartsWith("DH", StringComparison.OrdinalIgnoreCase))
            {
                //ArrayList orderCodes = GetUniqueOrderCodes(content);
                await ProcessDuyetDHCommand_TanLong(e.Channel, content, UserName);
            }
            else if (content.StartsWith("BG", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
            }
            else if (content.StartsWith("GBG", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
            }
            else if (content.StartsWith("HUY", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
            }
            else if (content.StartsWith("RES", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
            }
            else if (content.StartsWith("XOA", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
            }
            else if (content.StartsWith("KC", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
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
        private static async Task ProcessDuyetDHCommand_TanLong(DiscordChannel channel, string command, string userName)
        {
            ArrayList orderCodes = GetUniqueOrderCodes(command);
            await channel.SendMessageAsync("Vui lòng chờ giây lát...");

            foreach (string code in orderCodes)
            {
                bool isApproved = CheckOrderApproval_TanLong(code);
                if (isApproved)
                {
                    await channel.SendMessageAsync($"Đơn hàng {code} đã được duyệt, không được phép duyệt lại.");
                    Console.WriteLine("Order already approved");
                }
                else
                {
                    string result = ApproveOrderQuery_TanLong(code, userName);

                    if (result == "OK")
                    {
                        await channel.SendMessageAsync($"Đơn hàng {code} đã được duyệt.");
                        Console.WriteLine($"Order {code} Approve!!.");
                    }
                    else if (result == "ODR_NOT_EXIST")
                    {
                        Console.WriteLine("Order not exist");
                        await channel.SendMessageAsync($"Đơn hàng {code} không tồn tại.");
                    }
                    else if (result == "APPROVAL_NOT_OPEN")
                    {
                        await channel.SendMessageAsync($"Chưa mở duyệt đơn hàng, vui lòng liên hệ admin.");
                        Console.WriteLine("Approval not open");
                    }
                    else if (result == "APPROVAL_CLOSED")
                    {
                        await channel.SendMessageAsync($"Đã khóa duyệt đơn hàng, vui lòng liên hệ admin.");
                        Console.WriteLine("Approval closed");
                    }
                    else
                    {
                        // Send alert to admin/mod channel if needed (replace channel ID)
                        Console.WriteLine("Order needs review");
                        await channel.SendMessageAsync($"Error: {result}");
                    }
                }
            }
        }

        private static string ApproveOrderQuery_TanLong(string orderCode, string UserName)
        {
            if (UseTest == true)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connSV1))
                    {
                        connection.Open();

                        SqlCommand command = new SqlCommand("dbo.ApprovalOrderTelegramV2", connection);
                        command.CommandType = CommandType.StoredProcedure;
                        // Set input parameters
                        command.Parameters.AddWithValue("@SoDH", orderCode);
                        command.Parameters.AddWithValue("@NguoiLap", UserName);

                        // Set output parameter
                        SqlParameter resultParam = new SqlParameter("@Result", SqlDbType.VarChar, 100);
                        resultParam.Direction = ParameterDirection.Output;
                        command.Parameters.Add(resultParam);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (resultParam.Value.ToString() == "Success")
                        {
                            return "OK";
                        }
                        else if (resultParam.Value.ToString() == "ODR_NOT_EXIST.")
                        {
                            return "ODR_NOT_EXIST";
                        }
                        else if (resultParam.Value.ToString() == "APPROVAL_NOT_OPEN.")
                        {
                            return "APPROVAL_NOT_OPEN";
                        }
                        else if (resultParam.Value.ToString() == "APPROVAL_CLOSED.")
                        {
                            return "APPROVAL_CLOSED";
                        }
                        else
                        {
                            return UserName + resultParam.Value.ToString(); ;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return ex.Message;
                }

            }
            else
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connSV1))
                    {
                        connection.Open();
                        SqlCommand command = new SqlCommand("dbo.ApprovalOrderTelegramV2", connection);
                        command.CommandType = CommandType.StoredProcedure;
                        // Set input parameters
                        command.Parameters.AddWithValue("@SoDH", orderCode);
                        command.Parameters.AddWithValue("@NguoiLap", UserName);

                        // Set output parameter
                        SqlParameter resultParam = new SqlParameter("@Result", SqlDbType.VarChar, 100);
                        resultParam.Direction = ParameterDirection.Output;
                        command.Parameters.Add(resultParam);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (resultParam.Value.ToString() == "Success")
                        {
                            return "OK";
                        }
                        else if (resultParam.Value.ToString() == "ODR_NOT_EXIST.")
                        {
                            return "ODR_NOT_EXIST";
                        }
                        else if (resultParam.Value.ToString() == "APPROVAL_NOT_OPEN.")
                        {
                            return "APPROVAL_NOT_OPEN";
                        }
                        else if (resultParam.Value.ToString() == "APPROVAL_CLOSED.")
                        {
                            return "APPROVAL_CLOSED";
                        }
                        else
                        {
                            return UserName + resultParam.Value.ToString(); ;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return ex.Message;
                }
            }
        }
        private static bool CheckOrderApproval_TanLong(string orderCode)
        {
            // Execute the query to check if the quotation is approved
            // Return true if approved, false otherwise
            if (UseTest == true)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connSV1))
                    {
                        connection.Open();
                        SqlCommand command = new SqlCommand("SELECT Duyet FROM MTDonHang WHERE SoDH = @orderCode", connection);
                        command.Parameters.AddWithValue("@orderCode", orderCode);
                        object result = command.ExecuteScalar();
                        return result != null && (bool)result;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return false;
                }
            }
            else
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connSV1))
                    {
                        connection.Open();
                        SqlCommand command = new SqlCommand("SELECT Duyet FROM MTDonHang WHERE SoDH = @orderCode", connection);
                        command.Parameters.AddWithValue("@orderCode", orderCode);
                        object result = command.ExecuteScalar();
                        return result != null && (bool)result;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return false;
                }
            }

        }
        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }

    }
}
