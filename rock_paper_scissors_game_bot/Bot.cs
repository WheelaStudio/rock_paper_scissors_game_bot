using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using rock_paper_scissors_game_bot.Utilities;
namespace rock_paper_scissors_game_bot
{
    public class Bot
    {
        private readonly Configuration configuration;
        private readonly DataManager dataManager;
        private TelegramBotClient bot;
        private readonly Random random = new();
        private readonly string[] answerItems = { "Камень🗿", "Ножницы✂️", "Бумага📄" };
        private const int answerItemsCount = 3;
        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                string GetCurrentStatistics(long id)
                {
                    return $"Текущий счёт: Вы: {dataManager.GetStatisticsParameter(id, StatisticsParameter.Win)} " +
                         $"Бот: {dataManager.GetStatisticsParameter(id, StatisticsParameter.Lose)}";
                }
                if (update.Type == UpdateType.Message)
                {
                    var message = update.Message!;
                    var from = message.From!;
                    var id = from.Id;
                    var chat = message.Chat;
                    if (message.Type == MessageType.Text)
                    {
                        var text = message.Text;
                        if (text == "/start")
                        {
                            var isExists = dataManager.Registration(id);
                            await bot.SendTextMessageAsync(chatId: chat,
                               $"{(isExists ? "С возвращением" : "Добро пожаловать")}🎉, {from.FirstName}!\n" +
                               $"Бот будет периодически выключаться в связи" +
                               $" с обслуживанием базы данных хостингом, о котором Вам сообщит админ🤴!\n{GetCurrentStatistics(id)}\n" +
                               $"Выберите предмет, который вы хотите показать, ниже:",
                            replyMarkup: AnswersItemsButtons);
                        }
                        else if (text == "/resetgamescore")
                        {
                            await bot.SendTextMessageAsync(chatId: chat, "Вы действительно хотите сбросить счёт?", replyMarkup: ResetStatisticsButtons);
                        }
                        else if (text.StartsWith("/setConfig(") && id == configuration.Admin_id)
                        {
                            try
                            {
                                var firstIndex = text.IndexOf('(') + 1;
                                var variable = text.Substring(firstIndex, text.IndexOf(')') - firstIndex);
                                var value = text.Split('=')[1];
                                configuration.SetConfigValue(variable, value);
                                await bot.SendTextMessageAsync(chatId: chat, $"Настройка {variable} установлена в значении {value}", replyMarkup: AnswersItemsButtons, replyToMessageId: message.MessageId);
                            }
                            catch (Exception e)
                            {
                                await bot.SendTextMessageAsync(chatId: chat, $"Ошибка: {e.Message}", replyMarkup: AnswersItemsButtons, replyToMessageId: message.MessageId);
                            }
                        }
                        else if (text == "/config" && id == configuration.Admin_id)
                        {
                            var config = configuration.Config;
                            var configView = "";
                            foreach (var i in config)
                                configView += $"{i.Key}={i.Value}\n";
                            await bot.
                                SendTextMessageAsync(chatId: chat,
                                $"Конфигурация бота:\n{configView}", replyMarkup: AnswersItemsButtons, replyToMessageId: message.MessageId);
                        }
                        else if (text.StartsWith("/alert=") && id == configuration.Admin_id)
                        {
                            var alert = text.Split("=")[1];
                            if (alert == "")
                            {
                                await bot.SendTextMessageAsync(chatId: chat, "Некорректное предупреждение!", replyToMessageId: message.MessageId, replyMarkup: AnswersItemsButtons);
                                return;
                            }
                            await SendAlertToAllUsers(alert);
                            await bot.SendTextMessageAsync(chatId: chat, "Предупреждение всем успешно отправлено!", replyMarkup: AnswersItemsButtons, replyToMessageId: message.MessageId);
                        }
                        else if (text == "/reconnect" && id == configuration.Admin_id)
                        {
                            dataManager.Reconnect();
                            await bot.SendTextMessageAsync(chatId: chat, "Повторное подключение прошло успешно!", replyMarkup: AnswersItemsButtons, replyToMessageId: message.MessageId);
                        }
                        else
                        {
                            var result = GetResult(text);
                            if (result == null)
                                await bot.SendTextMessageAsync(chatId: chat, "Неизвестный предмет или команда!", replyMarkup: AnswersItemsButtons, replyToMessageId: message.MessageId);
                            else
                            {
                                var value = result.Value;
                                if (value.result != null)
                                {
                                    dataManager.IncreaseStatisticsParameter(id, value.result.Value);
                                }
                                await bot.SendTextMessageAsync(chatId: chat, $"{value.text}\n{GetCurrentStatistics(id)}", replyMarkup: AnswersItemsButtons, replyToMessageId: message.MessageId);
                            }
                        }
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(chatId: chat, "Неподдерживаемый тип сообщения!", replyMarkup: AnswersItemsButtons, replyToMessageId: message.MessageId);
                    }
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    var callback = update.CallbackQuery!;
                    if (update.CallbackQuery!.Data! == "Да")
                    {
                        dataManager.ResetStatistics(callback.From!.Id);
                        await bot.EditMessageTextAsync(callback.Message!.Chat, callback.Message.MessageId, "Счёт успешно сброшен!");
                    }
                    else
                    {
                        await bot.EditMessageTextAsync(callback.Message!.Chat, callback.Message.MessageId, "Вы отменили сброс счёта!");
                    }
                }
            }
            catch { }
        }
        private InlineKeyboardMarkup ResetStatisticsButtons
        {
            get
            {
                return new InlineKeyboardMarkup(new[]
               {
                InlineKeyboardButton.WithCallbackData("Да"),
                InlineKeyboardButton.WithCallbackData("Нет")
            });
            }
        }
        private ReplyKeyboardMarkup AnswersItemsButtons
        {
            get
            {
                var list = new List<KeyboardButton>();
                foreach (var answer in answerItems)
                {
                    list.Add(new KeyboardButton(answer));
                }
                var reply = new ReplyKeyboardMarkup(list)
                {
                    ResizeKeyboard = true
                };
                return reply;
            }
        }
        private async Task SendAlertToAllUsers(string alert)
        {
            var adminId = configuration.Admin_id;
            foreach (var userID in dataManager.GetAllUsersIdentifiers())
            {
                if (userID != adminId)
                {
                    try
                    {
                        await bot.SendTextMessageAsync(chatId: userID, $"Сообщение от админа🤴:\n{alert}", replyMarkup: AnswersItemsButtons, disableNotification: true);
                    }
                    catch { }
                }
            }
        }
        private RoundResult? GetResult(string answer)
        {
            var userAnswer = Array.IndexOf(answerItems, answer);
            if (userAnswer == -1)
                return null;
            var botAnswer = random.Next(0, answerItemsCount);
            if (userAnswer == botAnswer)
                return new RoundResult($"Ничья😉\nБот выбрал тоже самое!");
            switch (userAnswer)
            {
                case 0:
                    if (botAnswer == 1)
                        return new RoundResult("Победа🎉\nБот выбрал ножницы✂️", StatisticsParameter.Win);
                    else if (botAnswer == 2)
                        return new RoundResult("Поражение🙁\nБот выбрал бумагу📄", StatisticsParameter.Lose);
                    break;
                case 1:
                    if (botAnswer == 0)
                        return new RoundResult("Поражение🙁\nБот выбрал камень🗿", StatisticsParameter.Lose);
                    else if (botAnswer == 2)
                        return new RoundResult("Победа🎉\nБот выбрал бумагу📄", StatisticsParameter.Win);
                    break;
                case 2:
                    if (botAnswer == 0)
                        return new RoundResult("Победа🎉\nБот выбрал камень🗿", StatisticsParameter.Win);
                    else if (botAnswer == 1)
                        return new RoundResult("Поражение🙁\nБот выбрал ножницы✂️", StatisticsParameter.Lose);
                    break;
            }
            return null;
        }
        private async Task SendMessageToAdmin(string msg) { await bot.SendTextMessageAsync(configuration.Admin_id, msg); }
        private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var msg = $"Произошла ошибка:\n{(configuration.Show_Debug_Info ? exception.ToString() : exception.Message)}";
            await SendMessageToAdmin(msg);
            Console.WriteLine(msg);
        }
        public Bot()
        {
            dataManager = DataManager.Instance;
            configuration = Configuration.Instance;
            bot = new(configuration.Bot_token);
            configuration.AddFieldValueChangedHandler(nameof(configuration.Bot_token), async () =>
            {
                await SendMessageToAdmin("Перезапустите бота в панели управления Heroku!");
            });
            configuration.AddFieldValueChangedHandler(nameof(configuration.Admin_id), async () =>
            {
                await SendMessageToAdmin("Вы стали администратором бота🤖!");
            });
        }
        public void Start()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
            };
            bot.StartReceiving(
               HandleUpdateAsync,
               HandleErrorAsync,
               receiverOptions
           );
            Console.WriteLine("Бот запущен!");
        }
    }
}