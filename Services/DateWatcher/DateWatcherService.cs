using System.Buffers;
using Scheder.Services.Database;
using Scheder.Services.JournalAPI;
using Scheder.TelegramInteractions.Commands.Other;
using Scheder.Tools;
using Scheder.Tools.Config;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheder.Services.DateWatcher;

public class DateWatcherService {
    private static readonly SchedChanges SchedHandler = new();
    private const int UpdateTimeCheck = 30; // Minutes
    private static bool _onStartupTimeSkipped;
    private static TelegramBotClient _bot = new(Env.TelegramToken!);
    
    public static void Run() {
        
        var thread = new Thread(void () =>
        {
            var lastMinute = -1;

            while (true)
            {
                var currentMinute = DateTime.Now.Minute;

                if (currentMinute % UpdateTimeCheck == 0 && currentMinute != lastMinute || !_onStartupTimeSkipped)
                {
                    _onStartupTimeSkipped = true;
                    lastMinute = currentMinute;
                    _ = OnEveryMinute();
                }
                Thread.Sleep(1800);
            }
        }) {
            IsBackground = true
        };

        thread.Start();
    }

    public static void Load(TelegramBotClient bot) {
        _bot = bot;
    }

    private static async Task OnEveryMinute() {
        var watchFor = await Memory.GetAllWithDayListeners();

        foreach (var (uid, isGroup) in watchFor) {
            await RunForUser(uid, isGroup);
        }
    }


    private static async Task RunForUser(long uid, bool isGroup) {
        var dayForUser = await GmtTool.GetCurrentDayWithGmt(uid, isGroup);
        
        IEntitySource memorySource = isGroup ? new GroupSource() : new UserSource();
        var days = await memorySource.GetDayListeners(uid);

        foreach (var dayList in days) {
            var day= dayList.Date;
            var dayHash = dayList.Hash;
            
            if (CodeBunch.IsDayHadPast(dayForUser)) {
                await memorySource.RemoveDayListener(uid, day);
                continue;
            }
            
            var bestDay = BestDayOption.GetFromJournalStringFormat(day);
            var (sched, exams, _) = await GetSched.GetSchedAndExams(uid, bestDay, isGroup);
            
            if (sched == null) {return;} // failed to parse. Skip
            exams ??= "";

            var msgText = SchedMessageBuilder.BuildMessage(sched, bestDay, exams, asChange: true);
            var newHashCode = CodeBunch.GetTextHash(msgText);
            
            if (newHashCode.Equals(dayHash)) {return;} // nothing changed
            Logger.Log.Information("[DateWatcher | {Uid} (isGroup: {IsGroup}] New HashCode: {NewHashCode})", uid, isGroup, newHashCode);
            
            await SchedHandler.ExecuteAsync(_bot, uid, dayList.ThreadId, msgText, _bot.GlobalCancelToken);
            
            await memorySource.UpdateDayListener(uid, new Memory.DayListener {
                Date = dayList.Date,
                Hash = dayList.Hash,
                ThreadId = dayList.ThreadId
            });

        }
    }
}