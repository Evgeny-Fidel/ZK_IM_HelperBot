﻿using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

string version = "1.0.5";
var autor = "";
string TokenTelegramAPI = "";
string TokenWeather = "";
string connStr = "";

bool AutoUpdate = true;     // Включение/отключение функции автообновления бота
int AutoUpdateMinete = 30;  // Частота проверки обновлений
bool Logs = true;           // Включение/отключение логирования приватных сообщений в консоль
bool WeatherLoc = true;     // Включение/отключение отправка погоды по геолокации

string DirectoryProg = Environment.CurrentDirectory;
string DirectorySettings = $"{DirectoryProg}/Settings";
string DirectoryLogs = $"{DirectorySettings}/Logs";
string LogFileUpdate = $"{DirectoryLogs}/Update.txt";
string LogFileErrorTGAPI = $"{DirectoryLogs}/Telegram_API.txt";
string LogFilePrivatMessage = $"{DirectoryLogs}/Privat_Message.txt";

bool Permit = false;

Directory.CreateDirectory(DirectorySettings);

if (System.IO.File.Exists($"{DirectorySettings}/Authentication.txt"))
{
    using (StreamReader reader = new($"{DirectorySettings}/Authentication.txt"))
    {
        string server = "";
        string database = "";
        string uid = "";
        string pwd = "";

        string? line;
        try
        {
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("Token.Telegram.API ="))
                {
                    line = line.Replace("Token.Telegram.API =", "");
                    TokenTelegramAPI = line.Replace(" ", "");
                }
                if (line.StartsWith("Token.Weather ="))
                {
                    line = line.Replace("Token.Weather =", "");
                    TokenWeather = line.Replace(" ", "");
                }
                if (line.StartsWith("Server ="))
                {
                    line = line.Replace("Server =", "");
                    server = line.Replace(" ", "");
                }
                if (line.StartsWith("Database ="))
                {
                    line = line.Replace("Database =", "");
                    database = line.Replace(" ", "");
                }
                if (line.StartsWith("Uid ="))
                {
                    line = line.Replace("Uid =", "");
                    uid = line.Replace(" ", "");
                }
                if (line.StartsWith("Pwd ="))
                {
                    line = line.Replace("Pwd =", "");
                    pwd = line.Replace(" ", "");
                }
                if (line.StartsWith("Autor ="))
                {
                    line = line.Replace("Autor =", "");
                    autor = line.Replace(" ", "");
                }
                if (line.StartsWith("Auto_Update ="))
                {
                    line = line.Replace("Auto_Update =", "");
                    AutoUpdate = Convert.ToBoolean(line.Replace(" ", ""));
                }
                if (line.StartsWith("Auto_Update_Minute ="))
                {
                    line = line.Replace("Auto_Update_Minute =", "");
                    AutoUpdateMinete = Convert.ToInt32(line.Replace(" ", ""));
                }
                if (line.StartsWith("Weather_Location ="))
                {
                    line = line.Replace("Weather_Location =", "");
                    WeatherLoc = Convert.ToBoolean(line.Replace(" ", ""));
                }
            }
            connStr = $@"Server={server};Database={database};Uid={uid};Pwd={pwd};";
        }
        catch
        {
            Console.WriteLine($"Возникла ошибка при счении настроек - {DirectorySettings}/Authentication.txt\n" +
                $"Для того чтобы сбросить файл найстроек, удалите его, запустите бота снова, он сгенирирует правильный файл, после этого, Вам нужно его заполнить.\n");
        }
    }
}
else
{
    Console.WriteLine($"Отсутствуют файлы аутентификации! Заполните значения по этому пути - {DirectorySettings}/Authentication.txt");
    System.IO.File.WriteAllText($"{DirectorySettings}/Authentication.txt", "" +
        "———————————————————————————Telegram API————————————————————————————\n" +
        "Token.Telegram.API = ЗАМЕНИТЕ_ЭТОТ_ТЕКСТ_НА_СВОЙ_ТОКЕН_TELEGRAM\n" +
        "Token.Weather = ЗАМЕНИТЕ_ЭТОТ_ТЕКСТ_НА_СВОЙ_ТОКЕН_OPENWEATHERMAP\n\n" +
        "——————————————————————————————MySQL————————————————————————————————\n" +
        "Server = IP_ВАШЕЙ_БД\n" +
        "Database = ИМЯ_ВАШЕЙ_БД\n" +
        "Uid = ЛОГИН\n" +
        "Pwd = ПАРОЛЬ\n\n" +
        "———————————————————————————Telegram BOT————————————————————————————\n" +
        "Autor = @evgeny_fidel\n" +
        "Auto_Update = true\n" +
        "Auto_Update_Minute = 30\n" +
        "Weather_Location = true" +
        "");
}


Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

MySqlConnection MySqlBase = new(connStr);
try
{
    MySqlBase.Open();
    Console.WriteLine($"Успешное подключение к БД =)");
    MySqlBase.Close();
}
catch
{
    MySqlBase.Close();
    Console.WriteLine($"Не удалось подключиться к БД =(");
}

var botClient = new TelegramBotClient(TokenTelegramAPI);
using var cts = new CancellationTokenSource();
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>()
};
botClient.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, receiverOptions, cts.Token);
var me = await botClient.GetMeAsync();
Console.WriteLine($"Вышел на смену: \"{botClient.GetMeAsync().Result.FirstName}\" @{botClient.GetMeAsync().Result.Username}\nВерсия бота: {version} | {DateTime.Now:dd.MM.yy | HH:mm:ss}");

if (System.IO.File.Exists($"{DirectoryProg}/Update ZKIMHelperBot.zip")) { System.IO.File.Delete($"{DirectoryProg}/Update ZKIMHelperBot.zip"); }
if (System.IO.File.Exists($"{DirectoryProg}/UpdaterProg.exe")) { System.IO.File.Delete($"{DirectoryProg}/UpdaterProg.exe"); }

if (Logs == true)
{
    Directory.CreateDirectory(DirectoryLogs);
    long MaxSize = 1 * 1024 * 1024 * 1024;
    MaxSize *= 2;
    try
    {
        long SizeLogFileUpdate = new FileInfo(LogFileUpdate).Length;
        long SizeLogFileErrorTGAPI = new FileInfo(LogFileErrorTGAPI).Length;
        long SizeLogFilePrivatMessage = new FileInfo(LogFilePrivatMessage).Length;
        if (SizeLogFileUpdate > MaxSize) { System.IO.File.Delete(LogFileUpdate); }
        if (SizeLogFileErrorTGAPI > MaxSize) { System.IO.File.Delete(LogFileErrorTGAPI); }
        if (SizeLogFilePrivatMessage > MaxSize) { System.IO.File.Delete(LogFilePrivatMessage); }
    }
    catch { }
    System.IO.File.AppendAllText(LogFileUpdate, $"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Начало логирования..\n");
    System.IO.File.AppendAllText(LogFileErrorTGAPI, $"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Начало логирования..\n");
    System.IO.File.AppendAllText(LogFilePrivatMessage, $"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Начало логирования..\n");

    Console.WriteLine($"\n" +
        $"——————————Settings——————————\n" +
        $"Autor = {autor}\n" +
        $"Auto_Update = {AutoUpdate}\n" +
        $"Auto_Update_Minute = {AutoUpdateMinete}\n" +
        $"Weather_Location = {WeatherLoc}\n");
}
if (AutoUpdate == true)
{
    //Timer timer = new(TimerCallback, null, 0, AutoUpdateMinete * 60 * 1000);
    TimerCallback callback = new(UpdateBot);
    Timer timer = new(callback, null, 0, AutoUpdateMinete * 60 * 1000);
}
Console.ReadLine();
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Type == UpdateType.Message && update?.Message?.Text != null)
    {
        await HandleMessage(botClient, update, update.Message);
    }
    if (update.Type == UpdateType.CallbackQuery)
    {
        await HandleCallbackQuery(botClient, update.CallbackQuery);
    }
    if (update.MyChatMember != null)
    {
        if (update.MyChatMember.NewChatMember.Status == ChatMemberStatus.Administrator)
        {
            await HandleMember(botClient, update, update.Message);
        }
    }
    if (update.Message != null)
    {
        if (update.Message.Type == MessageType.Location)
        {
            await HandleLocation(botClient, update.Message);
        }
        if (update.Message.Type == MessageType.ChatMemberLeft ||
            update.Message.Type == MessageType.ChatMembersAdded ||
            update.Message.Type == MessageType.ChatPhotoChanged ||
            update.Message.Type == MessageType.ChatPhotoDeleted ||
            update.Message.Type == MessageType.ChatTitleChanged ||
            update.Message.Type == MessageType.MessageAutoDeleteTimerChanged ||
            update.Message.Type == MessageType.MessagePinned)
        {
            await HandleSystemMessage(botClient, update, update.Message);
        }
    }
    return;
}

async Task HandleMessage(ITelegramBotClient botClient, Update update, Message message)
{
    if (message.Text.ToLower().StartsWith("/hello_text "))
    {
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Данная команда предназначена только для администрации чата..", disableNotification: true);
            return;
        }
        string SaveText = message.Text.Replace("/hello_text ", "");
        if (SaveText == " " || SaveText == "")
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Ошибка!\nНе указан текст для сохранения!", disableNotification: true);
            return;
        }
        if (SaveText.Length > 4000)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Ошибка!\nСлишком длинное сообщение, максимум 4000 символов, а у Вас {SaveText.Length}", disableNotification: true);
            return;
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"UPDATE BDGroup SET hello_text = '{SaveText}' WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Что-то пошло не так..\nПопробуйте чуточку позже!)", disableNotification: true);
        }
        MySqlBase.Close();
        return;
    }
    if (message.Text.ToLower().StartsWith("/mute_text "))
    {
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Данная команда предназначена только для администрации чата..", disableNotification: true);
            return;
        }
        string SaveText = message.Text.Replace("/mute_text ", "");
        if (SaveText == " " || SaveText == "")
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Ошибка!\nНе указан текст для сохранения!", disableNotification: true);
            return;
        }
        if (SaveText.Length > 4000)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Ошибка!\nСлишком длинное сообщение, максимум 4000 символов, а у Вас {SaveText.Length}", disableNotification: true);
            return;
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"UPDATE BDGroup SET mute_text = '{SaveText}' WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Что-то пошло не так..\nПопробуйте чуточку позже!)", disableNotification: true);
        }
        MySqlBase.Close();
        return;
    }
    if (message.Text.ToLower().StartsWith("/rmute_text "))
    {
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Данная команда предназначена только для администрации чата..", disableNotification: true);
            return;
        }
        string SaveText = message.Text.Replace("/rmute_text ", "");
        if (SaveText == " " || SaveText == "")
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Ошибка!\nНе указан текст для сохранения!", disableNotification: true);
            return;
        }
        if (SaveText.Length > 4000)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Ошибка!\nСлишком длинное сообщение, максимум 4000 символов, а у Вас {SaveText.Length}", disableNotification: true);
            return;
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"UPDATE BDGroup SET rmute_text = '{SaveText}' WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Что-то пошло не так..\nПопробуйте чуточку позже!)", disableNotification: true);
        }
        MySqlBase.Close();
        return;
    }
    

    message.Text = message.Text.ToLower();
    if (Logs == true && message.Chat.Type == ChatType.Private)
    {
        string TextMes = message.Text;
        Console.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | {message.From.Id} - @{message.From.Username} | {TextMes.Replace("\n", " ")}");
        using (var File = new StreamWriter(LogFilePrivatMessage, true))
        {
            File.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | {message.From.Id} - @{message.From.Username} | {TextMes.Replace("\n", " ")}");
        }

    }
    try
    {
        if ($"@{message.From.Username.ToLower() ?? ""}" == autor)
        {
            if (message.Text.StartsWith("/permit_true"))
            {
                try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
                try
                {
                    MySqlBase.Open();
                    string cmdsql = $"UPDATE BDGroup SET permit = '1' WHERE id = '{message.Chat.Id}';";
                    MySqlCommand command = new(cmdsql, MySqlBase);
                    command.ExecuteNonQuery();
                    var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
                    await Task.Delay(1000);
                    await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
                }
                catch
                {
                    await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
                }
                MySqlBase.Close();
                return;
            }
            if (message.Text.StartsWith("/permit_false"))
            {
                try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
                try
                {
                    MySqlBase.Open();
                    string cmdsql = $"UPDATE BDGroup SET permit = '0' WHERE id = '{message.Chat.Id}';";
                    MySqlCommand command = new(cmdsql, MySqlBase);
                    command.ExecuteNonQuery();
                    var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
                    await Task.Delay(1000);
                    await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
                }
                catch
                {
                    await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
                }
                MySqlBase.Close();
                return;
            }
            if (message.Text.StartsWith("/bd"))
            {
                string hello_text = "-";
                string mute_text = "-";
                string rmute_text = "-";
                try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
                string Text = "";
                try
                {
                    MySqlBase.Open();
                    string cmdsql = $"SELECT * FROM BDUser;";
                    MySqlCommand command = new(cmdsql, MySqlBase);
                    MySqlDataReader reader = command.ExecuteReader();
                    Text = $"БД Пользователей:\n" +
                        $"ID|Usename|FirstName|LastName\n";
                    while (reader.Read())
                    {
                        string id = reader.GetString("id");
                        string username = reader.GetString("username");
                        string firstname = reader.GetString("firstname");
                        string lastname = reader.GetString("lastname");

                        if (id == "") { id = "-"; }
                        if (username == "") { username = "-"; } else { username = $"@{username}"; }
                        if (firstname == "") { firstname = "-"; }
                        if (lastname == "") { lastname = "-"; }

                        Text = $"{Text}{id}|{username}|{firstname}|{lastname}\n";
                    }
                    MySqlBase.Close();
                    MySqlBase.Open();
                    cmdsql = $"SELECT * FROM BDGroup;";
                    command = new MySqlCommand(cmdsql, MySqlBase);
                    reader = command.ExecuteReader();
                    Text = $"{Text}\nБД Групп:\n" +
                        $"ID|Title|Type|AutoWeatherLoc|Permit|DSM|Hello|HelloText|MuteText|RmuteText\n";
                    while (reader.Read())
                    {
                        hello_text = "-";
                        mute_text = "-";
                        rmute_text = "-";
                        string id = reader.GetString("id");
                        string title = reader.GetString("title");
                        string type = reader.GetString("type");
                        string AutoWeatherLoc = reader.GetString("auto_weather_loc");
                        string permitbd = reader.GetString("permit");
                        string DSM = reader.GetString("dsm");
                        string HelloBD = reader.GetString("hello");
                        try { hello_text = reader.GetString("hello_text").Replace("\n", " "); } catch { }
                        try { mute_text = reader.GetString("mute_text").Replace("\n", " "); } catch { }
                        try { rmute_text = reader.GetString("rmute_text").Replace("\n", " "); } catch { }

                        if (id == "") { id = "-"; }
                        if (title == "") { title = "-"; }
                        if (type == "") { type = "-"; }

                        Text = $"{Text}{id}|{title}|{type}|{AutoWeatherLoc}|{permitbd}|{DSM}|{HelloBD}|{hello_text}|{mute_text}|{rmute_text}\n";
                    }
                    var chunks = Text.Chunk(4096).Select(chunk => string.Join("", chunk)).ToList();

                    foreach (var chunk in chunks)
                        await botClient.SendTextMessageAsync(message.Chat, $"{chunk}", disableNotification: true);
                }
                catch
                {
                    await botClient.SendTextMessageAsync(message.Chat, $"Произошла ошибка..", disableNotification: true);
                }
                MySqlBase.Close();
                return;
            }
            if (message.Text.StartsWith("/up"))
            {
                try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
                if (Logs == true)
                {
                    using var File = new StreamWriter(LogFileUpdate, true);
                    File.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Начинаем проверку наличия новых версий..");
                }
                var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"🌐 Начинаем проверку наличия новых версий..", disableNotification: true);
                try
                {
                    using (var client = new WebClient())
                    {
                        string latestVersion = client.DownloadString("https://gaffer-prog.evgeny-fidel.ru/zk_im_helperbot/");
                        if (!latestVersion.Contains(version))
                        {
                            if (Logs == true)
                            {
                                using var File = new StreamWriter(LogFileUpdate, true);
                                File.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Вышла новая версия, пробуем скачать..");
                            }
                            await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"✅ Действительно, вышла новая версия, пробую скачать..");
                            Console.WriteLine("Вышла новая версия бота! Начинаем обновление..");
                            client.DownloadFile("https://gaffer-prog.evgeny-fidel.ru/download/459/", DirectoryProg + @"/Update ZKIMHelperBot.zip");
                            client.DownloadFile("https://gaffer-prog.evgeny-fidel.ru/download/110/", DirectoryProg + @"/UpdaterProg.exe");
                            if (Logs == true)
                            {
                                using var File = new StreamWriter(LogFileUpdate, true);
                                File.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Файлы обновления успешно скачались, пробуем обновиться..");
                            }
                            await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"✅ Все скачалось, сейчас обновлюсь!)");
                            Process.Start(DirectoryProg + @"/UpdaterProg.exe");
                            Environment.Exit(0);
                        }
                        else
                        {
                            if (Logs == true)
                            {
                                using var File = new StreamWriter(LogFileUpdate, true);
                                File.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Новых версий нет, работаем в прежнем режиме..");
                            }
                            await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"✅ Новых версий нет");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Logs == true)
                    {
                        using var File = new StreamWriter(LogFileUpdate, true);
                        File.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Произошла ошибка: {ex.Message}");
                    }
                    await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"❌ Произошла ошибка..");
                }
                return;
            }
        }
    }
    catch { }

    if (message.Text.StartsWith("ботя"))
    {
        string TextBot = message.Text;
        if (TextBot.Contains("перезапуск") || TextBot.Contains("рестарт"))
        {
            message.Text = "!start";
        }
        if(TextBot.Contains("помощь") || TextBot.Contains("помоги") || TextBot.Contains("помочь") || TextBot.Contains("хелп"))
        {
            message.Text = "!help";
        }
        if (TextBot.Contains("погод"))
        {
            message.Text = "!weather_im";
        }
        if (TextBot.Contains("настройка погод") || TextBot.Contains("настройки погод"))
        {
            message.Text = "!setting_weather";
        }
        if (TextBot.Contains("телефон"))
        {
            message.Text = "!phone";
        }
        if (TextBot.Contains("праздник") || TextBot.Contains("шар") || TextBot.Contains("помочь") || TextBot.Contains("игр"))
        {
            message.Text = "!airlemons";
        }
        if (TextBot.Contains("автобус"))
        {
            message.Text = "!bus";
        }
        if (TextBot.Contains("чат"))
        {
            message.Text = "!chats";
        }
    }

    if (message.Text.StartsWith("/start") || message.Text == "!start")
    {
        string Hello = $"Привет! Я бот \"{botClient.GetMeAsync().Result.FirstName}\"\n\n" +
        $"☀️ Я умею показывать погоду - \"Ботя, погода\"\n" +
        $"☎️ Нужные номера телефонов нашего ЖК - \"Ботя, телефоны\"\n" +
        $"🥳 Всё для праздника, воздушные шарики, игрушки - \"Ботя, праздник\"\n" +
        $"💬 Лучшие чаты ЖК - \"Ботя, чаты\"\n" +
        $"🚌 Расписание автобуса - \"Ботя, автобус\"\n" +
        $"❓ Забыл что я умею? - \"Ботя, помощь\"\n\n" +
        $"⬇️ Так же все мои команды доступны по кнопке команд (рядом с кнопкой стикеров) или по команде /help";

        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        await botClient.SendTextMessageAsync(message.Chat.Id, Hello, disableNotification: true);

        if (message.Chat.Type == ChatType.Private)
        {
            try
            {
                MySqlBase.Open();
                try
                {
                    string cmdsql = $"INSERT INTO BDUser (id, username, firstname, lastname) VALUES ('{message.From.Id}', '{message.From.Username}', '{message.From.FirstName}', '{message.From.LastName}');";
                    MySqlCommand command = new(cmdsql, MySqlBase);
                    command.ExecuteNonQuery();
                }
                catch
                {
                    string cmdsql = $"UPDATE BDUser SET username = '{message.From.Username}', firstname = '{message.From.FirstName}', lastname = '{message.From.LastName}' WHERE id = '{message.From.Id}';";
                    MySqlCommand command = new(cmdsql, MySqlBase);
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                Console.WriteLine("При добавлении в БД нового пользователя, произошла ошибка.");
            }
            MySqlBase.Close();
        }

        return;
    }
    if (message.Text.StartsWith("/help") || message.Text == "!help")
    {
        string Mes = $"" +
            $"/start - перезапуск бота;\n" +
            $"/help - доступные команды;\n" +
            $"\n" +
            $"/weather_im - погода в ЖК ИМ или отправь геолокацию, скажу какая погода в твоем районе;\n" +
            $"/phone - все номера телефонов;\n" +
            $"/bus - расписание автобуса;\n" +
            $"/airlemons - всё для праздника, воздушные шарики, игрушки;\n" +
            $"/chats - чаты ЖК;\n" +
            $"\nИли просто попроси: Ботя + то, что тебя интересует;\n" +
            $"\n⬇️ Так же все мои команды доступны по кнопке команд (рядом с кнопкой стикеров)";

        if (message.Text.StartsWith("/")) { try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { } }
        await botClient.SendTextMessageAsync(message.Chat.Id, Mes, disableNotification: true);
        return;
    }
    if (message.Text.StartsWith("/info"))
    {
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        string TextMes = "";
        string Username = "";
        string FirstName = "";
        string LastName = "";
        string ID = "";
        ChatMember chatMember;
        if (message.ReplyToMessage != null) // Проверка об ответном сообщении
        {
            Username = message.ReplyToMessage.From.Username;
            FirstName = message.ReplyToMessage.From.FirstName;
            LastName = message.ReplyToMessage.From.LastName;
            ID = $"{message.ReplyToMessage.From.Id}";
            chatMember = await botClient.GetChatMemberAsync(message.Chat.Id, message.ReplyToMessage.From.Id);
        }
        else
        {
            Username = message.From.Username;
            FirstName = message.From.FirstName;
            LastName = message.From.LastName;
            ID = $"{message.From.Id}";
            chatMember = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        }
        TextMes = $"" +
            $"Chat Title: {message.Chat.Title}\n" +
            $"Chat ID: {message.Chat.Id}\n" +
            $"\n" +
            $"From Username: @{Username}\n" +
            $"From Name: {FirstName} {LastName}\n" +
            $"From ID: {ID}\n" +
            $"From Member: {chatMember.Status}";
        try
        {
            /*try // Пользователь
            {
                MySqlBase.Open();

                var IDUser = message.From.Id;
                if (message.ReplyToMessage != null)
                {
                    IDUser = message.ReplyToMessage.From.Id;
                }
                string cmdsql = $"SELECT * FROM BDUser WHERE id = '{IDUser}';";
                MySqlCommand command = new(cmdsql, MySqlBase);
                MySqlDataReader reader = command.ExecuteReader();
                string saveurl = "";
                string saveurlval = "";
                string TestMes = "";
                while (reader.Read())
                {
                    saveurl = reader.GetString("saveurl");
                    saveurlval = reader.GetString("saveurlval");
                    TestMes = reader.GetString("TestMes");
                }
                MySqlBase.Close();
                TextMes = $"{TextMes}\n\n" +
                    $"Информация из БД по пользователю:\n" +
                    $"SaveUrl: {saveurl}\n" +
                    $"SaveURLVal: {saveurlval}\n" +
                    $"TestMes: {TestMes}";
            }
            catch { MySqlBase.Close(); }*/
            try // Группа
            {
                MySqlBase.Open();
                string cmdsql = $"SELECT * FROM BDGroup WHERE id = '{message.Chat.Id}';";
                MySqlCommand command = new(cmdsql, MySqlBase);
                MySqlDataReader reader = command.ExecuteReader();
                string Permit = "";
                string AutoWeatherLoc = "";
                string DSM = "";
                string HelloBD = "";
                string hello_text = "null";
                string mute_text = "null";
                string rmute_text = "null";
                while (reader.Read())
                {
                    Permit = reader.GetString("permit");
                    AutoWeatherLoc = reader.GetString("auto_weather_loc");
                    DSM = reader.GetString("dsm");
                    HelloBD = reader.GetString("hello");
                    try { hello_text = reader.GetString("hello_text"); } catch { }
                    try { mute_text = reader.GetString("mute_text"); } catch { }
                    try { rmute_text = reader.GetString("rmute_text"); } catch { }
                }
                MySqlBase.Close();
                TextMes = $"{TextMes}\n\n" +
                    $"Информация из БД по группе:\n" +
                    $"Permit: {Permit}\n" +
                    $"AutoWeatherLoc: {AutoWeatherLoc}\n" +
                    $"DSM: {DSM}\n" +
                    $"Hello: {HelloBD}\n" +
                    $"HelloText: {hello_text}\n" +
                    $"MuteText: {mute_text}\n" +
                    $"RmuteText: {rmute_text}";
            }
            catch { MySqlBase.Close(); }
        }
        catch { }
        TextMes = $"{TextMes}\n\n" +
            $"Разработчик: @evgeny_fidel\n" +
            $"Версия бота: {version}\n";
        var chunks = TextMes.Chunk(4096).Select(chunk => string.Join("", chunk)).ToList();

        foreach (var chunk in chunks)
            await botClient.SendTextMessageAsync(message.Chat, $"{chunk}", disableNotification: true);
        return;
    }

    if (message.Text.StartsWith("/weather_im") || message.Text == "!weather_im")
    {
        if (message.Text.StartsWith("/")) { try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { } }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }

        var mes = await botClient.SendTextMessageAsync(message.Chat, "Секунду, сейчас сбегаю и посмотрю! 🏃‍♂️", disableNotification: true);
        try
        {
            string Country = "🇷🇺";
            string City = "Императорские Мытищи";
            string Lat = "55.95";
            string Lon = "37.68";
            string Text = "";

            string Smiley = "";
            string SmileyWeather = "";
            string url = $"https://api.openweathermap.org/data/2.5/weather?lat={Lat}&lon={Lon}&units=metric&mode=xml&appid={TokenWeather}&lang=ru";

            WebClient client = new();
            var xml = client.DownloadString(url);
            XDocument xdoc = XDocument.Parse(xml);
            XElement? Temperature = xdoc.Element("current").Element("temperature");
            XAttribute? TemperatureVal = Temperature.Attribute("value");

            XElement? Weather = xdoc.Element("current").Element("weather");
            XAttribute? WeatherVal = Weather.Attribute("value");

            XElement? Humidity = xdoc.Element("current").Element("humidity");
            XAttribute? HumidityVal = Humidity.Attribute("value");

            XElement? Pressure = xdoc.Element("current").Element("pressure");
            XAttribute? PressureVal = Pressure.Attribute("value");
            double PressureValue = Convert.ToDouble(PressureVal.Value) * 0.750064;
            PressureValue = Math.Round(PressureValue, 0);

            XElement? Wind = xdoc.Element("current").Element("wind").Element("speed");
            XAttribute? WindVal = Wind.Attribute("value");

            var WeatherValue = WeatherVal.Value;
            double Temp = 0;
            try
            {
                Temp = Convert.ToDouble(TemperatureVal.Value);
            }
            catch { }

            Temp = Math.Round(Temp, 0);
            if (Temp == -0)
            {
                Temp = 0;
            }

            WeatherSmileAll(Temp, ref Smiley, WeatherValue, ref SmileyWeather);

            try { WeatherValue = WeatherValue.Substring(0, 1).ToUpper() + WeatherValue.Substring(1); } catch { }
            Text = $"{Text}\n\n{Country} {City}: {Temp}°C {Smiley}\n" +
                $"💦 Влажность: {HumidityVal.Value}%\n" +
                $"🧭 Давление: {PressureValue} мм рт. ст.\n" +
                $"💨 Скорость ветра: {WindVal.Value} м/с\n" +
                $"{SmileyWeather} {WeatherValue}";

            await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"{Text}");
        }
        catch
        {
            await botClient.EditMessageTextAsync(message.Chat, mes.MessageId, $"К сожалению произошла ошибка, попробуйте чуточку позже 😔");
        }
        return;
    }
    if (message.Text.StartsWith("/setting_weather") || message.Text == "!setting_weather")
    {
        if (message.Text.StartsWith("/")) { try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { } }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        string Smiley = "";
        string WeatherValue = "All";
        string SmileyWeather = "";
        string Mes = "Настройки температуры:\n";
        for (double Temp = -30; Temp <= 45; Temp += 5)
        {
            WeatherSmileAll(Temp, ref Smiley, WeatherValue, ref SmileyWeather);
            Mes += $"{Smiley} {Temp}°C\n";
        }
        await botClient.SendTextMessageAsync(message.Chat.Id, $"{Mes}\nНастройки погодных условий:\n{SmileyWeather}", disableNotification: true);
        return;
    }
    if (message.Text.StartsWith("/phone") || message.Text == "!phone")
    {
        if (message.Text.StartsWith("/")) { try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { } }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        string PhoneNumber = System.IO.File.ReadAllText($@"{DirectorySettings}\Phone.txt");
        await botClient.SendTextMessageAsync(message.Chat.Id, PhoneNumber, disableNotification: true);
        return;
    }
    if (message.Text.StartsWith("/airlemons") || message.Text == "!airlemons")
    {
        if (message.Text.StartsWith("/")) { try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { } }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        InlineKeyboardMarkup inlineKeyboard = new(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl(text: "🌐 Сайт", $"https://airlemons.ru/"),
            },
            new[]
            {
                InlineKeyboardButton.WithUrl(text: "📱 Telegram", "https://t.me/airlemons"),
                InlineKeyboardButton.WithUrl(text: "📱 WhatsApp", "https://wa.me/+79035002225"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "📍 Показать геолокацию магазина", "Airlemons_Shop"),
            },

        });
        await botClient.SendTextMessageAsync(message.Chat.Id,
            $"🎊 О видимо у Вас скоро праздник, поздравляю!\n" +
            $"Лучшие товары для праздника, игрушки, воздушные шарики и не только, есть у этих ребят!\nВсе их контакты доступны по кнопкам.\n" +
            $"А так же их магазин прямо у нас в ЖК! 😉",
            replyMarkup: inlineKeyboard, disableNotification: true);

        return;
    }
    if (message.Text.StartsWith("/bus") || message.Text == "!bus")
    {
        if (message.Text.StartsWith("/")) { try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { } }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        await using Stream stream = System.IO.File.OpenRead(@$"{DirectorySettings}/Bus.jpg");
        await botClient.SendPhotoAsync(message.Chat.Id, new InputOnlineFile(stream, @$"{DirectorySettings}/Bus.jpg"), disableNotification: true);
        //caption: "My Photo",
        return;
    }
    if (message.Text.StartsWith("/chats") || message.Text == "!chats")
    {
        if (message.Text.StartsWith("/")) { try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { } }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        InlineKeyboardMarkup inlineKeyboard = new(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl(text: "🚘 Попутчики", $"https://t.me/joinchat/UCXryFo9UJQ_Ge4F"),
                InlineKeyboardButton.WithUrl(text: "💨 Отдам даром", $"https://t.me/vtoraya_gizn"),
            },
            new[]
            {
                InlineKeyboardButton.WithUrl(text: "🏰 Недвижимость", $"https://t.me/+ToOceSd3Nzdr9yOu"),
                InlineKeyboardButton.WithUrl(text: "🔁 Купи - продай", $"https://t.me/imml_trade"),
            },
        });
        await botClient.SendTextMessageAsync(message.Chat.Id,
            $"🚘 Попутчики - найди себе попутчика или сам им стань!\n" +
            $"💨 Отдам даром - любители халявы или Вы просто очень щедрый, тогда Вам сюда\n" +
            $"🏰 Недвижимость - тут явно только богатые обитают\n" +
            $"🔁 Купи|продай - маленькое Авито\n" +
            $"\n",
            replyMarkup: inlineKeyboard, disableNotification: true);

        return;
    }


    if (message.Text.StartsWith("/hello_text_clearn"))
    {
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Данная команда предназначена только для администрации чата..", disableNotification: true);
            return;
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"UPDATE BDGroup SET hello_text = NULL WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Что-то пошло не так..\nПопробуйте чуточку позже!)", disableNotification: true);
        }
        MySqlBase.Close();
        return;
    }
    if (message.Text.StartsWith("/mute_text_clearn"))
    {
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Данная команда предназначена только для администрации чата..", disableNotification: true);
            return;
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"UPDATE BDGroup SET mute_text = NULL WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Что-то пошло не так..\nПопробуйте чуточку позже!)", disableNotification: true);
        }
        MySqlBase.Close();
        return;
    }
    if (message.Text.StartsWith("/rmute_text_clearn"))
    {
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Данная команда предназначена только для администрации чата..", disableNotification: true);
            return;
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"UPDATE BDGroup SET rmute_text = NULL WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Что-то пошло не так..\nПопробуйте чуточку позже!)", disableNotification: true);
        }
        MySqlBase.Close();
        return;
    }
    if (message.Text.StartsWith("/mute"))
    {
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        if (message.ReplyToMessage == null) // Проверка об ответном сообщении
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Вы не указали пользователя!", disableNotification: true);
            return;
        }
        ChatMember chatMember = await botClient.GetChatMemberAsync(message.Chat.Id, message.ReplyToMessage.From.Id);
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);

        string NameUserBlock = "";
        string NameUserGood = "";

        if (message.ReplyToMessage.From.Username == null)
        {
            NameUserBlock = $"{message.ReplyToMessage.From.FirstName} {message.ReplyToMessage.From.LastName ?? " "}";
            NameUserBlock = NameUserBlock.Replace("  ", "");
            NameUserBlock = $"<a href=\"tg://user?id={message.ReplyToMessage.From.Id}\">{NameUserBlock}</a>";
        }
        else
        {
            NameUserBlock = $"@{message.ReplyToMessage.From.Username}";
        }
        if (message.From.Username == null)
        {
            NameUserGood = $"{message.From.FirstName} {message.From.LastName ?? " "}";
            NameUserGood = NameUserGood.Replace("  ", "");
            NameUserGood = $"<a href=\"tg://user?id={message.From.Id}\">{NameUserGood}</a>";
        }
        else
        {
            NameUserGood = $"@{message.From.Username}";
        }

        if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator) // Проверка на админа
        {
            await botClient.SendTextMessageAsync(message.Chat, $"{NameUserBlock} явлеется администратором данной группы, замутить его невозможно..", disableNotification: true, parseMode: ParseMode.Html);
            return;
        }

        var SplitMes = message.Text.Split(' ').Last();
        SplitMes = Regex.Replace(SplitMes, @"\D+", "");

        int MaxMinuteMember = 1;
        int MuteMinute;
        if (chatMemberYou.Status == ChatMemberStatus.Administrator || chatMemberYou.Status == ChatMemberStatus.Creator)
        {
            if (SplitMes == "")
            {
                MuteMinute = 1;
            }
            else
            {
                MuteMinute = Convert.ToInt32(SplitMes);
            }
        }
        else
        {
            if (SplitMes == "")
            {
                MuteMinute = 1;
            }
            else
            {
                MuteMinute = Convert.ToInt32(SplitMes);
                if (MuteMinute > MaxMinuteMember)
                {
                    MuteMinute = MaxMinuteMember;
                    int Opachki = Convert.ToInt32(SplitMes);
                }
            }
        }
        await botClient.RestrictChatMemberAsync(message.Chat.Id, message.ReplyToMessage.From.Id, permissions: new ChatPermissions
        {
            CanSendMessages = false,
            CanSendMediaMessages = false,
            CanSendOtherMessages = false
        }, DateTime.UtcNow.AddMinutes(MuteMinute));

        string Text = "";
        try
        {
            MySqlBase.Open();
            string cmdsql = $"SELECT * FROM BDGroup WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                try { Text = reader.GetString("mute_text"); } catch { }
            }
        }
        catch { }
        MySqlBase.Close();

        if (Text == "" || Text == " ")
        {
            Text = $"❗️У {NameUserBlock} заблокированы пальцы в чате на {MuteMinute} {GetCorrectWordForm(MuteMinute, "минуту", "минуты", "минут")}\nСкажем спасибо {NameUserGood} 😉";
        }
        else
        {
            Text = Text.Replace("%username_block%", $"{NameUserBlock}").Replace("%username_good%", $"{NameUserGood}").Replace("%time%", $"{MuteMinute} {GetCorrectWordForm(MuteMinute, "минуту", "минуты", "минут")}");
        }

        await botClient.SendTextMessageAsync(message.Chat, Text, disableNotification: true, replyToMessageId: message.ReplyToMessage.MessageId, parseMode: ParseMode.Html);
        return;
    }
    if (message.Text.StartsWith("/rmute"))
    {
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (message.ReplyToMessage == null) // Проверка об ответном сообщении
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Вы не указали пользователя!", disableNotification: true);
            return;
        }
        ChatMember chatMember = await botClient.GetChatMemberAsync(message.Chat.Id, message.ReplyToMessage.From.Id);
        string NameUserBlock = "";
        string NameUserGood = "";
        if (message.ReplyToMessage.From.Username == null)
        {
            NameUserBlock = $"{message.ReplyToMessage.From.FirstName} {message.ReplyToMessage.From.LastName ?? " "}";
            NameUserBlock = NameUserBlock.Replace("  ", "");
            NameUserBlock = $"<a href=\"tg://user?id={message.ReplyToMessage.From.Id}\">{NameUserBlock}</a>";
        }
        else
        {
            NameUserBlock = $"@{message.ReplyToMessage.From.Username}";
        }
        if (message.From.Username == null)
        {
            NameUserGood = $"{message.From.FirstName} {message.From.LastName ?? " "}";
            NameUserGood = NameUserGood.Replace("  ", "");
            NameUserGood = $"<a href=\"tg://user?id={message.From.Id}\">{NameUserGood}</a>";
        }
        else
        {
            NameUserGood = $"@{message.From.Username}";
        }

        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"{NameUserGood} я рад за Вашу доблесть и отвагу, но {NameUserBlock} может разблокировать только администрация чата!", disableNotification: true, replyToMessageId: message.ReplyToMessage.MessageId, parseMode: ParseMode.Html);
            return;
        }
        await botClient.RestrictChatMemberAsync(message.Chat.Id, message.ReplyToMessage.From.Id, permissions: new ChatPermissions
        {
            CanSendMessages = true,
            CanSendMediaMessages = true,
            CanSendOtherMessages = true
        });

        string Text = "";
        try
        {
            MySqlBase.Open();
            string cmdsql = $"SELECT * FROM BDGroup WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                try { Text = reader.GetString("rmute_text"); } catch { }
            }
        }
        catch { }
        MySqlBase.Close();

        if (Text == "" || Text == " ")
        {
            Text = $"У {NameUserBlock} разблокированы пальцы в чате!";
        }
        else
        {
            Text = Text.Replace("%username_block%", $"{NameUserBlock}").Replace("%username_good%", $"{NameUserGood}");
        }


        await botClient.SendTextMessageAsync(message.Chat, Text, disableNotification: true, replyToMessageId: message.ReplyToMessage.MessageId, parseMode: ParseMode.Html);
        return;
    }
    if (message.Text.StartsWith("/auto_weather_loc_off"))
    {
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Данная команда предназначена только для администрации чата..", disableNotification: true);
            return;
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"UPDATE BDGroup SET auto_weather_loc = '0' WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
        }
        MySqlBase.Close();
        return;
    }
    if (message.Text.StartsWith("/auto_weather_loc_on"))
    {
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Данная команда предназначена только для администрации чата..", disableNotification: true);
            return;
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"UPDATE BDGroup SET auto_weather_loc = '1' WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Что-то пошло не так.. Попробуйте чуточку позже!)", disableNotification: true);
        }
        MySqlBase.Close();
        return;
    }
    if (message.Text.StartsWith("/dsm_off"))
    {
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Данная команда предназначена только для администрации чата..", disableNotification: true);
            return;
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"UPDATE BDGroup SET dsm = '0' WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Что-то пошло не так..\nПопробуйте чуточку позже!)", disableNotification: true);
        }
        MySqlBase.Close();
        return;
    }
    if (message.Text.StartsWith("/dsm_on"))
    {
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Данная команда предназначена только для администрации чата..", disableNotification: true);
            return;
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"UPDATE BDGroup SET dsm = '1' WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Что-то пошло не так..\nПопробуйте чуточку позже!)", disableNotification: true);
        }
        MySqlBase.Close();
        return;
    }
    if (message.Text.StartsWith("/hello_off"))
    {
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Данная команда предназначена только для администрации чата..", disableNotification: true);
            return;
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"UPDATE BDGroup SET hello = '0' WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Что-то пошло не так..\nПопробуйте чуточку позже!)", disableNotification: true);
        }
        MySqlBase.Close();
        return;
    }
    if (message.Text.StartsWith("/hello_on"))
    {
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        ChatMember chatMemberYou = await botClient.GetChatMemberAsync(message.Chat.Id, message.From.Id);
        if (chatMemberYou.Status != ChatMemberStatus.Administrator && chatMemberYou.Status != ChatMemberStatus.Creator)
        {
            await botClient.SendTextMessageAsync(message.Chat, $"Данная команда предназначена только для администрации чата..", disableNotification: true);
            return;
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"UPDATE BDGroup SET hello = '1' WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
            var mes = await botClient.SendTextMessageAsync(message.Chat.Id, $"✅ Настройки обновлены!", disableNotification: true);
            await Task.Delay(1000);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
        catch
        {
            await botClient.SendTextMessageAsync(message.Chat, $"❌Что-то пошло не так..\nПопробуйте чуточку позже!)", disableNotification: true);
        }
        MySqlBase.Close();
        return;
    }
}

async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
{
    switch (callbackQuery.Data)
    {
        case "Airlemons_Shop":
            {
                await botClient.SendLocationAsync(callbackQuery.Message.Chat.Id, latitude: 55.959036f, longitude: 37.679662f, disableNotification: true, replyToMessageId: callbackQuery.Message.MessageId);
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Тенистый бульвар, дом 11", disableNotification: true);
                break;
            }
    }
}

async Task HandleMember(ITelegramBotClient botClient, Update update, Message message)
{
    try
    {
        string ChatID = "";
        string ChatTitle = "";
        string ChatTypeS = "";
        string Mes = "";

        int chek = 0;

        if (update.MyChatMember == null && update.Message != null)
        {
            ChatID = $"{message.Chat.Id}";
            ChatTitle = $"{message.Chat.Title}";
            ChatTypeS = $"{message.Chat.Type}";
            Mes = "✅ Настройки группы обновленны!";
            chek++;
        }
        else
        {
            if (update.MyChatMember.Chat.Type == ChatType.Group || update.MyChatMember.Chat.Type == ChatType.Supergroup)
            {
                ChatID = $"{update.MyChatMember.Chat.Id}";
                ChatTitle = $"{update.MyChatMember.Chat.Title}";
                ChatTypeS = $"{update.MyChatMember.Chat.Type}";
                Mes = "✅ Бот подключен!";
            }
        }
        try
        {
            MySqlBase.Open();
            string cmdsql = $"INSERT INTO BDGroup (id, title, type) VALUES ('{ChatID}', '{ChatTitle}', '{ChatTypeS}');";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
        }
        catch
        {
            string cmdsql = $"UPDATE BDGroup SET title = '{ChatTitle}', type = '{ChatTypeS}' WHERE id = '{ChatID}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            command.ExecuteNonQuery();
        }
        MySqlBase.Close();

        var mes = await botClient.SendTextMessageAsync(ChatID, Mes, disableNotification: true);
        if (chek > 0)
        {
            await Task.Delay(1500);
            await botClient.DeleteMessageAsync(message.Chat.Id, mes.MessageId);
        }
    }
    catch { }
    return;
}

async Task HandleLocation(ITelegramBotClient botClient, Message message)
{
    if (WeatherLoc == true)
    {
        ChekPermitGroup(message, ref Permit);
        if (Permit == false) { return; }
        bool chek = false;
        if (message.Chat.Type == ChatType.Private) { chek = true; }
        else
        {
            try
            {
                MySqlBase.Open();
                string cmdsql = $"SELECT * FROM BDGroup WHERE id = '{message.Chat.Id}';";
                MySqlCommand command = new(cmdsql, MySqlBase);
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var Auto_currency = reader.GetString("auto_weather_loc");
                    if (Auto_currency == "True")
                    {
                        chek = true;
                    }
                }
            }
            catch { }
        }
        if (chek == true)
        {
            try
            {
                var Lat = message.Location.Latitude;
                var Lon = message.Location.Longitude;
                string url = $"https://api.openweathermap.org/data/2.5/weather?lat={Lat}&lon={Lon}&units=metric&mode=xml&appid={TokenWeather}&lang=ru";
                string Smiley = "";
                string SmileyWeather = "";

                WebClient client = new();
                var xml = client.DownloadString(url);
                XDocument xdoc = XDocument.Parse(xml);
                XElement? Temperature = xdoc.Element("current").Element("temperature");
                XAttribute? TemperatureVal = Temperature.Attribute("value");

                XElement? Weather = xdoc.Element("current").Element("weather");
                XAttribute? WeatherVal = Weather.Attribute("value");

                XElement? Humidity = xdoc.Element("current").Element("humidity");
                XAttribute? HumidityVal = Humidity.Attribute("value");

                XElement? Pressure = xdoc.Element("current").Element("pressure");
                XAttribute? PressureVal = Pressure.Attribute("value");
                double PressureValue = Convert.ToDouble(PressureVal.Value) * 0.750064;
                PressureValue = Math.Round(PressureValue, 0);

                XElement? Wind = xdoc.Element("current").Element("wind").Element("speed");
                XAttribute? WindVal = Wind.Attribute("value");

                string WeatherValue = WeatherVal.Value;
                double Temp = 0;
                try
                {
                    Temp = Convert.ToDouble(TemperatureVal.Value);
                }
                catch { }

                Temp = Math.Round(Temp, 0);
                if (Temp == -0)
                {
                    Temp = 0;
                }

                WeatherSmileAll(Temp, ref Smiley, WeatherValue, ref SmileyWeather);

                try { WeatherValue = WeatherValue.Substring(0, 1).ToUpper() + WeatherValue.Substring(1); } catch { }
                string Text = $"{Smiley} В данном районе: {Temp}°C\n💦 Влажность: {HumidityVal.Value}%\n🧭 Давление: {PressureValue} мм рт. ст.\n💨 Скорость ветра: {WindVal.Value} м/с\n{SmileyWeather} {WeatherValue}";

                await botClient.SendTextMessageAsync(message.Chat, Text, disableNotification: true, replyToMessageId: message.MessageId);
            }
            catch { }
        }
    }
    MySqlBase.Close();
    return;
}

async Task HandleSystemMessage(ITelegramBotClient botClient, Update update, Message message)
{
    bool DSM = false;
    bool Hello = false;
    string HelloText = "";
    if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
    {
        try
        {
            MySqlBase.Open();
            string cmdsql = $"SELECT * FROM BDGroup WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                var DSMBD = reader.GetString("dsm");
                var MesHello = reader.GetString("hello");
                try { HelloText = reader.GetString("hello_text"); } catch { }
                if (DSMBD == "True")
                {
                    DSM = true;
                }
                if (MesHello == "True")
                {
                    Hello = true;
                }
            }
        }
        catch { }
        MySqlBase.Close();
    }
    if (update.Message.Type == MessageType.ChatTitleChanged)
    {
        await HandleMember(botClient, update, update.Message);
    }
    if (DSM == true)
    {
        try { await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId); } catch { }
    }
    if (update.Message.Type == MessageType.ChatMembersAdded && Hello == true)
    {
        try
        {
            string NameUser = "";
            if (message.NewChatMembers[0].Username == null)
            {
                NameUser = $"{message.NewChatMembers[0].FirstName} {message.NewChatMembers[0].LastName ?? " "}";
                NameUser = NameUser.Replace("  ", "");
                NameUser = $"<a href=\"tg://user?id={message.NewChatMembers[0].Id}\">{NameUser}</a>";
            }
            else
            {
                NameUser = $"@{message.NewChatMembers[0].Username}";
            }
            if (HelloText == "" || HelloText == " ")
            {
                HelloText = $"{NameUser} добро пожаловать!✌️\nЯ бот {botClient.GetMeAsync().Result.FirstName}, стараюсь помогать всем в этом чатике. Мои возможности - /help";
            }
            else
            {
                HelloText = HelloText.Replace("%username%", $"{NameUser}").Replace("%botname%", $"{botClient.GetMeAsync().Result.FirstName}");
            }
            await botClient.SendTextMessageAsync(message.Chat.Id, $"{HelloText}", disableNotification: true, parseMode: ParseMode.Html);
        }
        catch { }
    }
    MySqlBase.Close();
    return;
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Ошибка:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    if (Logs == true)
    {
        using (var File = new StreamWriter(LogFileErrorTGAPI, true))
        {
            File.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | {ErrorMessage}");
        }
    }
    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;

}

void UpdateBot(object? state)
{
    if (Logs == true)
    {
        using var File = new StreamWriter(LogFileUpdate, true);
        File.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Начинаем проверку наличия новых версий..");
    }
    try
    {
        using (var client = new WebClient())
        {
            string latestVersion = client.DownloadString("https://gaffer-prog.evgeny-fidel.ru/zk_im_helperbot/");
            if (!latestVersion.Contains(version))
            {
                if (Logs == true)
                {
                    using var File = new StreamWriter(LogFileUpdate, true);
                    File.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Вышла новая версия, пробуем скачать..");
                }
                Console.WriteLine("Вышла новая версия бота! Начинаем обновление..");
                client.DownloadFile("https://gaffer-prog.evgeny-fidel.ru/download/459/", DirectoryProg + @"/Update ZKIMHelperBot.zip");
                client.DownloadFile("https://gaffer-prog.evgeny-fidel.ru/download/110/", DirectoryProg + @"/UpdaterProg.exe");
                if (Logs == true)
                {
                    using var File = new StreamWriter(LogFileUpdate, true);
                    File.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Файлы обновления успешно скачались, пробуем обновиться..");
                }
                Process.Start(DirectoryProg + @"/UpdaterProg.exe");
                Environment.Exit(0);
            }
            else
            {
                if (Logs == true)
                {
                    using var File = new StreamWriter(LogFileUpdate, true);
                    File.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Новых версий нет, работаем в прежнем режиме..");
                }
            }
        }
    }
    catch (Exception ex)
    {
        if (Logs == true)
        {
            using var File = new StreamWriter(LogFileUpdate, true);
            File.WriteLine($"{DateTime.Now:dd.MM.yy | HH:mm:ss} | Произошла ошибка: {ex.Message}");
        }
    }
}

void WeatherSmileAll(double Temp, ref string Smiley, string WeatherValue, ref string SmileyWeather)
{
    if (Temp <= -30) { Smiley = "🥶🥶🥶"; }
    else if (Temp > -30 && Temp <= -25) { Smiley = "🥶🥶"; }
    else if (Temp > -25 && Temp <= -20) { Smiley = "🥶"; }
    else if (Temp > -20 && Temp <= -15) { Smiley = "😫"; }
    else if (Temp > -15 && Temp <= -10) { Smiley = "😖"; }
    else if (Temp > -10 && Temp <= -5) { Smiley = "😣"; }
    else if (Temp > -5 && Temp <= 0) { Smiley = "😬"; }
    else if (Temp > 0 && Temp <= 5) { Smiley = "😕"; }
    else if (Temp > 5 && Temp <= 10) { Smiley = "😐"; }
    else if (Temp > 10 && Temp <= 15) { Smiley = "😏"; }
    else if (Temp > 15 && Temp <= 20) { Smiley = "😌"; }
    else if (Temp > 20 && Temp <= 25) { Smiley = "😊"; }
    else if (Temp > 25 && Temp <= 30) { Smiley = "☺️"; }
    else if (Temp > 30 && Temp <= 35) { Smiley = "🥵"; }
    else if (Temp > 35 && Temp <= 40) { Smiley = "🥵🥵"; }
    else if (Temp > 40) { Smiley = "🥵🥵🥵"; }

    var weatherEmoji = new Dictionary<string, string>
                {
                    {"ясно", "☀️"},
                    {"небольшая облачность", "🌤"},
                    {"переменная облачность", "🌤"},
                    {"облачно с прояснениями", "🌥"},
                    {"пасмурно", "☁️"},
                    {"небольшой дождь", "🌦"},
                    {"небольшой проливной дождь", "🌧"},
                    {"сильный дождь", "🌧"},
                    {"гроза", "⛈"},
                    {"гроза с дождём", "⛈"},
                    {"гроза с небольшим дождём", "⛈"},
                    {"гроза с сильным дождём", "⛈"},
                    {"небольшой снег", "🌨"},
                    {"небольшой снегопад", "🌨"},
                    {"небольшой снег с дождём", "🌨"},
                    {"сильный снег", "❄️"},
                    {"снегопад", "❄️"},
                    {"снег", "❄️"},
                    {"туман", "🌫"},
                    {"плотный туман", "🌫"},
                    {"торнадо","🌪" },
                };
    string smileyWeather;
    if (WeatherValue == "All")
    {
        SmileyWeather = "";
        foreach (var item in weatherEmoji)
        {
            SmileyWeather += $"{item.Value} {item.Key}\n";
        }
    }
    else
    {
        if (weatherEmoji.TryGetValue(WeatherValue, out smileyWeather))
        {
            SmileyWeather = smileyWeather;
        }
        else
        {
            SmileyWeather = "❔";
        }
    }
}

void ChekPermitGroup(Message message, ref bool Permit)
{
    if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
    {
        try
        {
            MySqlBase.Open();
            string cmdsql = $"SELECT * FROM BDGroup WHERE id = '{message.Chat.Id}';";
            MySqlCommand command = new(cmdsql, MySqlBase);
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                var Auto_currency = reader.GetString("permit");
                if (Auto_currency == "True")
                {
                    Permit = true;
                }
                else
                {
                    Permit = false;
                    botClient.SendTextMessageAsync(message.Chat.Id, $"⛔️ Отказано! Мои функции - заблокированы!\nАдминистрация группы, свяжитесь с {autor} для разблокировки!", disableNotification: true);
                }
            }
        }
        catch { Permit = true; }
    }
    else
    {
        Permit = true;
    }
    MySqlBase.Close();
}

static string GetCorrectWordForm(int number, string form1, string form2, string form3)
{
    if (number % 100 >= 11 && number % 100 <= 14)
    {
        return form3;
    }
    switch (number % 10)
    {
        case 1:
            return form1;
        case 2:
        case 3:
        case 4:
            return form2;
        default:
            return form3;
    }
}