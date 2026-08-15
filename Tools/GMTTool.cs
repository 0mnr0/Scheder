using Scheder.Services.Database;

namespace Scheder.Tools;

public class GmtTool
{
    public static async Task<int> Get(long uid, bool fromGroup = false)
    {
        if (fromGroup)
        {
            var linkedUser = await Memory.Group.GetUserObject(uid);
            return linkedUser?.GMT ?? 0;
        }
        
        var user = await Memory.User.GetUserAsync(uid);
        return user?.GMT ?? 0;
    }

    public static async Task<string> GetCurrentDayWithGmt(long uid, bool fromGroup = false) {
        var timeShift =  await Get(uid, fromGroup);
        var currentDate = DateTime.Now.AddHours(timeShift);
        
        return currentDate.ToString("yyyy-MM-dd");
    }
}