using Discord_Bot.config;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

//using System;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Net;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;


namespace Discord_Bot
{
    internal class Program
    {
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }

        static Mutex mutex = null;
        static async Task Main(string[] args)
        {
            // ---- check for two instances ----
            const string appName = "DiscordBot_TanLong_Mutex";
            bool createdNew;
            mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                Console.WriteLine("Another instance is already running.");
                return;
            }
            // ---- original logic ----
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
            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromSeconds(30)
            });
            await Client.ConnectAsync();
            await Task.Delay(-1);   //Assure bot stay online as long as program is on

        }
        public static string connSV1;
        public static readonly ulong generalchat_id = 1376470323867684906;
        public static readonly ulong order_approval = 1376483154172448778;
        public static readonly ulong testing_zone = 1377083562125430915;

        private static async Task HandleCommand(string content, MessageCreateEventArgs e, string UserName, string displayName)
        {
            string normalized = content.Trim().ToLower();

            if (content.StartsWith("!start", StringComparison.OrdinalIgnoreCase) || content.StartsWith("!help", StringComparison.OrdinalIgnoreCase))
            {
                await ShowInstruction(e.Channel.Id,UserName);
                await Task.Delay(500);
            }
            else if (content.StartsWith("DH", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
                await ProcessDuyetDHCommand_TanLong(e.Channel, orderCodes, UserName, displayName);
                await Task.Delay(500);
            }
            else if (content.StartsWith("BG", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
                await ProcessDuyetBGCommand_TanLong(e.Channel, orderCodes, UserName, displayName);
                await Task.Delay(500);
            }
            else if (content.StartsWith("GBG", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
                await ExtendQuotationCommand_TanLong(e.Channel, orderCodes, UserName, displayName);
                await Task.Delay(500);
            }
            else if (content.StartsWith("HUY", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
                await ProcessHuyDuyetDHCommand_TanLong(e.Channel, orderCodes, UserName, displayName);
                await Task.Delay(500);
            }
            else if (content.StartsWith("KH", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
                await ProcessRESETKHCommand_TanLong(e.Channel, orderCodes, UserName, displayName);
                await Task.Delay(500);
            }
            else if (content.StartsWith("XOA", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
                await ProcessXoaLSXCommand_TanLong(e.Channel, orderCodes, UserName, displayName);
                await Task.Delay(500);
            }
            else if (content.StartsWith("KC", StringComparison.OrdinalIgnoreCase))
            {
                ArrayList orderCodes = GetUniqueOrderCodes(content);
                await Task.Delay(500); // Add your processing logic here if needed
            }
            else if (content.StartsWith("!clear", StringComparison.OrdinalIgnoreCase))
            {
                var messages = await e.Channel.GetMessagesAsync(100);
                await e.Channel.DeleteMessagesAsync(messages);
                await e.Channel.SendMessageAsync("🧹 Đã xóa 100 tin nhắn gần nhất.").ContinueWith(async msg =>
                {
                    await Task.Delay(2000);
                    await (await msg).DeleteAsync();
                });
                Console.WriteLine($"{UserName} ask to delete 100 recent messages");
            }
            else if (normalized == "y" || normalized == "n" || normalized == "yes" || normalized == "no")
            {
                return;
            }
            else
            {
                await e.Channel.SendMessageAsync("Sai cú pháp, vui lòng gõ !help để biết thêm chi tiết");
            }
        }

        private static async Task OnMessageCreated(DiscordClient client, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || string.IsNullOrWhiteSpace(e.Message.Content))
                return;
            string content = e.Message.Content.Trim();
            var member = await e.Guild.GetMemberAsync(e.Author.Id);

            string displayName = string.IsNullOrWhiteSpace(member.Nickname) ? member.Username : member.Nickname;
            //string UserName = $"{displayName}#{e.Author.Discriminator}"; //hiển thị reneon369#0 thay vì Ren
            //string UserName = displayName; // or e.Author.Username if you prefer global hiển thị reneon369
            //string UserName = member.Nickname ?? member.Username; sẽ gây lỗi nếu code như hiện tại (check DiscordID trong DMNhanvien
            string UserName = member.Username;
            if (!CheckTextLength(content))
            {
                Console.WriteLine("Code 400: Max length exceeded");
                await e.Channel.SendMessageAsync("Bot chỉ duyệt TỐI ĐA 133 đơn hàng cùng lúc, vui lòng chia nhỏ ra để chạy");
                await Task.Delay(500);
                return;
            }
            //string normalized = content.Trim().ToLower();
            string botMention = $"<@{client.CurrentUser.Id}>";
            string botMentionNick = $"<@!{client.CurrentUser.Id}>";
            if (content.StartsWith(botMention)) content = content.Substring(botMention.Length).Trim();
            else if (content.StartsWith(botMentionNick)) content = content.Substring(botMentionNick.Length).Trim();

            string normalized = content.ToLower();

            if (e.Channel.Id == order_approval || e.Channel.Id == testing_zone)
            {
                // process all commands freely without mention
                await HandleCommand(content, e, UserName, displayName);
            }
            else if (e.Message.MentionedUsers.Any(u => u.Id == client.CurrentUser.Id))
            {
                if (normalized.StartsWith("!start") || normalized.StartsWith("!help"))
                {
                    await ShowInstruction(e.Channel.Id, UserName);
                    await Task.Delay(500);
                }
                else
                {
                    await e.Channel.SendMessageAsync(
                        "Vui lòng chuyển sang kênh phù hợp để thực hiện thao tác:\n" +
                        "1. Chọn kênh order-approval để duyệt/hủy duyệt BG, ĐH hoặc xóa LSX.\n" +
                        "2. Chọn kênh adjust-quantity để điều chỉnh số lượng của nhật ký sx"
                    );
                }
            }
        }
        private static bool CheckTextLength(string content)
        {
            return content.Length <= 2000;
        }
        private static async Task ShowInstruction(ulong channelId, string userName)
        {
            var channel = await Client.GetChannelAsync(channelId);
            if (channel == null) return;

            string message =
                "Bot hỗ trợ các cú pháp sau:\n" +
                "1. Duyệt đơn hàng: `DH*DH-0010-0523`\n" +
                "2. Duyệt báo giá: `BG*BG-0123-0523`\n" +
                "3. Gia hạn báo giá (thêm 2 tháng): `GBG*BG-0123-0523`\n" +
                "4. Hủy duyệt đơn hàng: `HUY*DH-0123-0523`\n" +
                "5. Reset đẩy KHSX: `KH*KH-0123-0523`\n" +
                "6. Xóa LSX trong 1 đơn: `XOA*LSX-01234-0523`\n";
            // + "7. Đổi MaKC của lệnh: `KC:MãKC,LSX-01234-0523`\n"
            Console.WriteLine($"{userName} ask to show instruction");
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
        private static async Task ProcessDuyetDHCommand_TanLong(DiscordChannel channel, ArrayList orderCodes, string userName, string displayName)
        {
            //ArrayList orderCodes = GetUniqueOrderCodes(command);
            List<string> responses = new List<string>();
            List<string> errcode = new List<string>();
            foreach (string code in orderCodes)
            {
                if (CheckOrderApproval_TanLong(code))
                {
                    responses.Add($"Đơn hàng `{code}` đã được duyệt, không được phép duyệt lại.");
                    errcode.Add($"{userName} request for {code} error 405: already approved");
                }
                else
                {
                    string result = ApproveOrderQuery_TanLong(code, userName);
                    if (result == "OK")
                    {
                        responses.Add($"Đơn hàng `{code}` đã được duyệt bởi `{displayName}` .");
                        errcode.Add($"{userName} request for {code} OK");
                    }
                    else if (result == "ODR_NOT_EXIST")
                    {
                        responses.Add($"`{displayName}` đề nghị duyệt đơn hàng `{code}` không tồn tại.");
                        errcode.Add($"{userName} request for {code} error 404: not exist");
                    }
                    else if (result == "APPROVAL_NOT_OPEN")
                    {
                        responses.Add($"Chưa mở duyệt đơn hàng `{code}` mà `{displayName}` đề nghị, vui lòng liên hệ admin.");
                        errcode.Add($"{userName} request for {code} error 401: not open");
                    }
                    else if (result == "APPROVAL_CLOSED")
                    {
                        responses.Add($"Đã khóa duyệt đơn hàng `{code}` mà `{displayName}` đề nghị, vui lòng liên hệ admin.");
                        errcode.Add($"{userName} request for {code} error 402: closed");
                    }
                    else if (result == "DISCORD_ID_NOT_FOUND")
                    {
                        responses.Add($"Không tìm thấy Discord ID của `{displayName}` trong MYPACKSOFT, vui lòng liên hệ admin.");
                        errcode.Add($"{userName} request for `{code}` error 403: discord id not found");
                    }
                    else
                    {
                        responses.Add($"Đề nghị duyệt đơn hàng `{code}` của `{displayName}` bị lỗi: `{result}`");
                        errcode.Add($"{userName} request for {code} error 403: unknown error");
                    }
                }
            }
            string errmessage = string.Join("\n", errcode);
            Console.WriteLine(errmessage);
            string combinedMessage = string.Join("\n", responses);
            await channel.SendMessageAsync(combinedMessage);
        }
        private static string ApproveOrderQuery_TanLong(string orderCode, string UserName)
        {
            try
                {
                    using (SqlConnection connection = new SqlConnection(connSV1))
                    {
                        connection.Open();

                        SqlCommand command = new SqlCommand("dbo.ApprovalOrderTelegramV2", connection);
                        SqlCommand command2 = new SqlCommand("SELECT MaNV FROM DMNhanVien WHERE DiscordID = @UserName", connection);
                        command2.Parameters.AddWithValue("@UserName", UserName);
                        object result = command2.ExecuteScalar();
                        if (result != null)
                        {
                            string maNV = result.ToString();
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@SoDH", orderCode);
                            command.Parameters.AddWithValue("@NguoiLap", maNV);
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
                        else
                        {
                            return "DISCORD_ID_NOT_FOUND";
                        }
                }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return ex.Message;
                }
        }
        private static bool CheckOrderApproval_TanLong(string orderCode)
        {
            // Execute the query to check if the quotation is approved
            // Return true if approved, false otherwise
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
        private static async Task ExtendQuotationCommand_TanLong(DiscordChannel channel, ArrayList quotationCodes, string userName, string displayName)
        {
            //ArrayList orderCodes = GetUniqueOrderCodes(command);
            List<string> responses = new List<string>();
            List<string> errcode = new List<string>();
            foreach (string code in quotationCodes)
            {
                string result = ExtendQuotationProcedure_TanLong(code, userName, displayName);
                if (result == "OK")
                {
                    responses.Add($"Báo giá `{code}` đã gia hạn thêm 2 tháng bởi {displayName}.");
                    errcode.Add($"{userName} extend request for {code} OK");
                }
                else
                {
                    responses.Add($"Báo giá `{code}` được {displayName} đề nghị bị lỗi.");
                    errcode.Add($"{userName} request for {code} error 403: unknown");
                }
            }
            string errmessage = string.Join("\n", errcode);
            Console.WriteLine(errmessage);
            string combinedMessage = string.Join("\n", responses);
            await channel.SendMessageAsync(combinedMessage);
        }
        private static string ExtendQuotationProcedure_TanLong(string quotationCode,string userName, string displayName)
        {
        try
                {
                    using (SqlConnection connection = new SqlConnection(connSV1))
                    {
                        connection.Open();
                        using (SqlCommand command = new SqlCommand("ExtendQuotation", connection))
                        {
                            command.CommandType = System.Data.CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@SoCT", quotationCode);

                            // Define the @Result parameter as output
                            SqlParameter resultParameter = new SqlParameter("@Result", SqlDbType.VarChar, 100);
                            resultParameter.Direction = ParameterDirection.Output;
                            command.Parameters.Add(resultParameter);

                            // Execute the stored procedure
                            command.ExecuteNonQuery();

                            // Get the value of the output parameter
                            string result = (string)command.Parameters["@Result"].Value;

                            return result;
                        }
}
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
        }
        private static async Task ProcessDuyetBGCommand_TanLong(DiscordChannel channel, ArrayList orderCodes, string userName, string displayName)
        {
            List<string> responses = new List<string>();
            List<string> errcode = new List<string>();
            var interactivity = Client.GetInteractivity();

            foreach (string code in orderCodes)
            {
                if (CheckQuotationApproval_TanLong(code))
                {   //hủy duyệt
                    await channel.SendMessageAsync($"`{displayName}`, bạn có chắc chắn muốn **hủy duyệt báo giá `{code}`** không? Trả lời `Y/N` để xác nhận.");
                    var confirmation = await interactivity.WaitForMessageAsync(
                        m => m.Author.Username == userName && m.ChannelId == channel.Id,
                        TimeSpan.FromSeconds(15)
                        );
                    var accepted = new HashSet<string> { "y", "yes" };
                    string input = confirmation.Result.Content.Trim().ToLower();

                    if (confirmation.TimedOut || !accepted.Contains(input))
                    {
                        responses.Add($"❌ Hủy duyệt báo giá `{code}` đã bị hủy bỏ.");
                        errcode.Add($"{userName} request for {code} UNAPPROVAL: CANCELED");
                        continue;
                    }

                    UnapproveQuotation_TanLong(code);
                    responses.Add($"Báo giá `{code}` đã được `{displayName}` hủy.");
                    errcode.Add($"{userName} request for {code} UNAPPROVAL: OK");
                }
                else //duyệt
                {
                    ApproveQuotation_TanLong(code);
                    responses.Add($"Báo giá `{code}` đã được `{displayName}` duyệt.");
                    errcode.Add($"{userName} request for {code}: OK");
                }
            }
            string errmessage = string.Join("\n", errcode);
            Console.WriteLine(errmessage);
            string combinedMessage = string.Join("\n", responses);
            await channel.SendMessageAsync(combinedMessage);
        }
        private static async Task ProcessHuyDuyetDHCommand_TanLong(DiscordChannel channel, ArrayList orderCodes, string userName, string displayName)
        {
            //ArrayList orderCodes = GetUniqueOrderCodes(command);
            List<string> responses = new List<string>();
            List<string> errcode = new List<string>();
            foreach (string code in orderCodes)
            {
                string result  = CancelApprovalOrder(code);
                if (result == "WBLPS_DATA_EXIST")
                {
                    responses.Add($"Đơn hàng `{code}` đã hủy duyệt bởi `{displayName}` lỗi do đã nhập phôi sóng");
                    errcode.Add($"{userName} request cancellation for {code} error 421: {result}");
                }
                else if (result == "DTKH_DATA_EXIST")
                {
                    responses.Add($"Đơn hàng `{code}` đã hủy duyệt bởi `{displayName}` lỗi do đã lập KHSX");
                    errcode.Add($"{userName} request cancellation for {code} error 422: {result}");
                }
                else if (result == "OK")
                {
                    responses.Add($"Đơn hàng `{code}` đã hủy duyệt bởi `{displayName}` thành công");
                    errcode.Add($"{userName} request cancellation for {code}: OK!!");
                }
                else
                {
                    responses.Add($"Đề nghị hủy duyệt `{code}` của `{displayName}` thất bại do lỗi");
                    errcode.Add($"{userName} request cancellation for {code} 423: unknown");
                }
            }
            string errmessage = string.Join("\n", errcode);
            Console.WriteLine(errmessage);
            string combinedMessage = string.Join("\n", responses);
            await channel.SendMessageAsync(combinedMessage);
        }
        private static bool CheckQuotationApproval_TanLong(string quotationCode)
        {
            // Execute the query to check if the quotation is approved
            // Return true if approved, false otherwise
            try
            {
                using (SqlConnection connection = new SqlConnection(connSV1))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("SELECT Duyet FROM MTBaoGia WHERE SoBG = @QuotationCode", connection);
                    command.Parameters.AddWithValue("@QuotationCode", quotationCode);
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
        
        private static void UnapproveQuotation_TanLong(string quotationCode)
        {
            // Execute the query to unapprove the quotation
            try
                {
                    using (SqlConnection connection = new SqlConnection(connSV1))
                    {
                        connection.Open();
                        SqlCommand command = new SqlCommand("UPDATE MTBaoGia SET Duyet = 0, NguoiDuyet = NULL WHERE SoBG = @QuotationCode", connection);
                        command.Parameters.AddWithValue("@QuotationCode", quotationCode);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            Console.WriteLine($"Báo giá {quotationCode} đã được hủy duyệt.");
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            Console.WriteLine($"Lỗi khi hủy duyệt báo giá.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
        }
        private static string ApproveQuotation_TanLong(string quotationCode)
        {
            try
                {
                    using (SqlConnection connection = new SqlConnection(connSV1))
                    {
                        connection.Open();
                        using (SqlCommand command = new SqlCommand("ApprovalQuotation", connection))
                        {
                            command.CommandType = System.Data.CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@SoCT", quotationCode);

                            // Define the @Result parameter as output
                            SqlParameter resultParameter = new SqlParameter("@Result", SqlDbType.VarChar, 100);
                            resultParameter.Direction = ParameterDirection.Output;
                            command.Parameters.Add(resultParameter);

                            // Execute the stored procedure
                            command.ExecuteNonQuery();

                            // Get the value of the output parameter
                            string result = (string)command.Parameters["@Result"].Value;

                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
        }
        private static string CancelApprovalOrder(string quotationCode)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connSV1))
                {
                    connection.Open();
                    SqlCommand getDtdhids = new SqlCommand(@"
                            SELECT DISTINCT d.DTDHID 
                            FROM DTLSX d 
                            JOIN MTLSX m ON d.MTLSXID = m.MTLSXID 
                            WHERE m.SoDH = @QuotationCode", connection);
                    getDtdhids.Parameters.AddWithValue("@QuotationCode", quotationCode);
                    List<Guid> dtdhids = new List<Guid>();
                    using (SqlDataReader reader = getDtdhids.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                                dtdhids.Add(reader.GetGuid(0));
                        }
                    }

                    // Step 2: Check if any of those DTDHIDs exist in WBLPS
                    foreach (var dtdhid in dtdhids)
                    {
                        SqlCommand checkWblps = new SqlCommand("SELECT TOP 1 1 FROM wblps WHERE DTDHID = @dtdhid", connection);
                        SqlCommand checkDTKH = new SqlCommand("SELECT TOP 1 1 FROM DTKH WHERE DTDHID = @dtdhid", connection);

                        checkWblps.Parameters.AddWithValue("@dtdhid", dtdhid);
                        checkDTKH.Parameters.AddWithValue("@dtdhid", dtdhid);

                        object result = checkWblps.ExecuteScalar();
                        object result2 = checkWblps.ExecuteScalar();

                        if (result != null)
                        {
                            return "WBLPS_DATA_EXIST";
                        }
                        if (result2 != null)
                        {
                            return "DTKH_DATA_EXIST";
                        }
                    }
                    SqlCommand command = new SqlCommand("DELETE FROM DTLSX WHERE MTLSXID = (SELECT MTLSXID FROM MTLSX WHERE SODH = @QuotationCode)", connection);
                    SqlCommand command2 = new SqlCommand("DELETE FROM MTLSX WHERE SoDH = @QuotationCode", connection);
                    SqlCommand command3 = new SqlCommand("UPDATE MTDonHang SET Duyet = 0, NguoiDuyet = NULL, LSX = NULL, NgayDuyet = NULL WHERE SoDH = @QuotationCode", connection);
                    SqlCommand command4 = new SqlCommand("UPDATE d SET d.Tinhtrang = NULL from DTDONHANG d JOIN MTDONHANG m on m.MTDHID = d.MTDHID WHERE m.SoDH = @QuotationCode", connection);

                    command.Parameters.AddWithValue("@QuotationCode", quotationCode);
                    command2.Parameters.AddWithValue("@QuotationCode", quotationCode);
                    command3.Parameters.AddWithValue("@QuotationCode", quotationCode);
                    command4.Parameters.AddWithValue("@QuotationCode", quotationCode);

                    int rowsAffected = command.ExecuteNonQuery();
                    int rowsAffected2 = command2.ExecuteNonQuery();
                    int rowsAffected3 = command3.ExecuteNonQuery();
                    int rowsAffected4 = command3.ExecuteNonQuery();

                    if (rowsAffected >= 0 && rowsAffected2 >= 0 && rowsAffected3 > 0 && rowsAffected4 > 0)
                    {
                        return "OK";
                    }
                    else
                    {
                        return "Error";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return ex.Message;
            }
        }
        private static async Task ProcessRESETKHCommand_TanLong(DiscordChannel channel, ArrayList orderCodes, string userName, string displayName)
        {
            List<string> responses = new List<string>();
            List<string> errcode = new List<string>();
            foreach (string code in orderCodes)
            {
                if (checkKHSX(code))
                {
                    string result = RESETKHSX_TanLong(code);
                    if (result == "OK")
                    {
                        responses.Add($"KHSX `{code}` đã reset bởi `{displayName}`");
                        errcode.Add($"{userName} reset request for {code}: OK!!");
                    }
                    else if (result == "Error")
                    {
                        responses.Add($"KHSX `{code}` đề nghị bởi `{displayName}` reset thất bại do chưa chuyển file");
                        errcode.Add($"{userName} reset request for {code} 431: Not yet Uploaded");
                    }
                }
                else
                {
                    responses.Add($"KHSX `{code}` đề nghị bởi `{displayName}` reset thất bại do lỗi");
                    errcode.Add($"{userName} reset request for {code} 432: unknown");
                }
            }
            string errmessage = string.Join("\n", errcode);
            Console.WriteLine(errmessage);
            string combinedMessage = string.Join("\n", responses);
            await channel.SendMessageAsync(combinedMessage);
        }
        private static bool checkKHSX(string orderCode)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connSV1))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("SELECT isExport FROM MTKH WHERE SoKH = @orderCode", connection);
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

        private static string RESETKHSX_TanLong(string orderCodes)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connSV1))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand("UPDATE MTKH SET isExport = 0, exporteddate = NULL  WHERE SoKH = @orderCodes", connection);

                    command.Parameters.AddWithValue("@orderCodes", orderCodes);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return "OK";
                    }
                    else
                    {
                        return "Error";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return ex.Message;
            }
        }
        private static async Task ProcessXoaLSXCommand_TanLong(DiscordChannel channel, ArrayList orderCodes, string userName, string displayName)
        {
            List<string> responses = new List<string>();
            List<string> errcode = new List<string>();
            foreach (string code in orderCodes)
            {
                if (checkLSX(code))
                {
                    string result = DELETELSX_TanLong(code);
                    if (result == "OK")
                    {
                        responses.Add($"`{code}` đã xoá thành công bởi `{displayName}`");
                        errcode.Add($"{userName} delete request for {code}: OK!!");
                    }
                    else if (result == "Error")
                    {
                        responses.Add($"`{code}` đề nghị xoá bởi `{displayName}` thất bại do đã tồn tại trong bảng cân đối");
                        errcode.Add($"{userName} delete request for {code} error code 441: exist in blvt or wblps");
                    }
                    else if (result == "No Data")
                    {
                        responses.Add($"`{code}` đề nghị xoá bởi `{displayName}` thất bại do không có dữ liệu");
                        errcode.Add($"{userName} delete request for {code} error code 442: no data");
                    }
                    else if (result == "Error404")
                    {
                        responses.Add($"`{code}` đề nghị xoá bởi `{displayName}` thất bại do không xóa được dữ liệu trong 4 bảng");
                        errcode.Add($"{userName} delete request for {code} error code 443: did not delete in all 4 table");
                    }
                }
                else
                {
                    responses.Add($"`{code}` yêu cầu xoá bởi `{displayName}` thất bại do lỗi");
                    errcode.Add($"{userName} delete request for {code} error code 442: unknown");
                }
            }
            string errmessage = string.Join("\n", errcode);
            Console.WriteLine(errmessage);
            string combinedMessage = string.Join("\n", responses);
            await channel.SendMessageAsync(combinedMessage);
        }

        private static string DELETELSX_TanLong(string orderCode)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connSV1))
                {
                    connection.Open();

                    // Get MTLSXID
                    SqlCommand cmd = new SqlCommand("SELECT MTLSXID FROM DTLSX WHERE SoLSX = @orderCode", connection);
                    cmd.Parameters.AddWithValue("@orderCode", orderCode);
                    object result = cmd.ExecuteScalar();

                    if (result == null)
                        return "No Data";

                    string mtlsxid = result.ToString();

                    // Get DTDHID
                    SqlCommand getdtdhid = new SqlCommand("SELECT DTDHID FROM DTLSX WHERE SoLSX = @orderCode", connection);
                    getdtdhid.Parameters.AddWithValue("@orderCode", orderCode);
                    string dtdhid = getdtdhid.ExecuteScalar()?.ToString();

                    if (string.IsNullOrEmpty(dtdhid))
                        return "No Data";
                    SqlCommand getMTDHID = new SqlCommand(@"SELECT MTDHID FROM DTDONHANG WHERE DTDHID = @dtdhid", connection);
                    getMTDHID.Parameters.AddWithValue("@dtdhid", dtdhid);
                    string mtdhid = getMTDHID.ExecuteScalar()?.ToString();
                    if (string.IsNullOrEmpty(mtdhid))
                        return "No Data";

                    int rowCount = checkRowCount(orderCode, mtlsxid);
                    if (rowCount < 1)
                        return "No Data";
                    else if (rowCount == 1)
                    {
                        using (SqlTransaction transaction = connection.BeginTransaction())
                        { try
                            {
                                SqlCommand dellsx = new SqlCommand("DELETE FROM DTLSX WHERE DTDHID = @dtdhid", connection, transaction);
                                SqlCommand delmtlsx = new SqlCommand("DELETE FROM MTLSX WHERE MTLSXID = @mtlsxid", connection, transaction);
                                SqlCommand deldh = new SqlCommand("DELETE FROM DTDonhang WHERE DTDHID = @dtdhid", connection, transaction);
                                SqlCommand delmtdh = new SqlCommand("DELETE FROM MTDonhang WHERE MTDHID = @mtdhid", connection, transaction);
                                dellsx.Parameters.AddWithValue("@dtdhid", dtdhid);
                                delmtlsx.Parameters.AddWithValue("@mtlsxid", mtlsxid);
                                deldh.Parameters.AddWithValue("@dtdhid", dtdhid);
                                delmtdh.Parameters.AddWithValue("@mtdhid", mtdhid);

                                int r1 = dellsx.ExecuteNonQuery();
                                int r2 = delmtlsx.ExecuteNonQuery();
                                int r3 = deldh.ExecuteNonQuery();
                                int r4 = delmtdh.ExecuteNonQuery();
                                Console.WriteLine(r1);
                                Console.WriteLine(r2);
                                Console.WriteLine(r3);
                                Console.WriteLine(r4);
                                if (r1 > 0 && r2 > 0 && r3 > 0 && r4 > 0)
                                {
                                    transaction.Commit();
                                    return "OK";
                                }
                                else
                                {
                                    transaction.Rollback();
                                    return "Error404";
                                }
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                Console.WriteLine("Transaction failed: " + ex.Message);
                                return "Error";
                            }
                        } 
                    }
                    else if (rowCount > 1)
                    {
                        SqlCommand delOne= new SqlCommand(@"
                                                            DELETE FROM DTLSX WHERE DTDHID = @dtdhid;
                                                            DELETE FROM DTDonhang WHERE DTDHID = @dtdhid;
                                                            ", connection);

                        delOne.Parameters.AddWithValue("@dtdhid", dtdhid);
                        int rowsAffected = delOne.ExecuteNonQuery();
                        return rowsAffected > 0 ? "OK" : "Error";
                    }
                        return "Error";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return ex.Message;
            }
        }

        private static bool checkLSX(string orderCode)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connSV1))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("SELECT DTDHID FROM DTLSX WHERE solsx = @orderCode", connection);
                    command.Parameters.AddWithValue("@orderCode", orderCode);
                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        string dtdhid = result.ToString();
                        SqlCommand ckwblps = new SqlCommand("SELECT TOP 1 1 FROM WBLPS WHERE DTDHID = @DTDHID", connection);
                        ckwblps.Parameters.AddWithValue("@DTDHID", dtdhid);
                        SqlCommand ckblvt = new SqlCommand("SELECT TOP 1 1 FROM BLVT WHERE DTDHID = @DTDHID", connection);
                        ckblvt.Parameters.AddWithValue("@DTDHID", dtdhid);
                        SqlCommand ckKHSX = new SqlCommand("SELECT TOP 1 1 FROM DTKH WHERE DTDHID = @DTDHID", connection);
                        ckKHSX.Parameters.AddWithValue("@DTDHID", dtdhid);

                        object ewblps = ckwblps.ExecuteScalar();
                        object eblvt = ckblvt.ExecuteScalar();
                        object eKHSX = ckKHSX.ExecuteScalar();

                        bool anyExists = ewblps != null || eblvt != null || eKHSX != null;
                        return !anyExists;
                    }
                    else return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }
        private static int checkRowCount(string orderCode, string mtlsxid)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connSV1))
                {
                    connection.Open();
                    SqlCommand rowcount = new SqlCommand("SELECT count(*) FROM DTLSX WHERE MTLSXID = @MTLSXID", connection);
                    rowcount.Parameters.AddWithValue("@MTLSXID", mtlsxid);
                    object result = rowcount.ExecuteScalar();
                    if ( result != null)
                    {
                        int numberOfRows = Convert.ToInt32(rowcount.ExecuteScalar());
                        return numberOfRows;
                    }
                    else return(0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (-1);
            }
        }
        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }

    }
}
