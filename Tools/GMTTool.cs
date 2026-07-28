using Scheder.Services.Database;

namespace Scheder.Tools;

public class GmtTool
{
    public static async Task<int> Get(long uid, bool fromGroup = false)
    {
        if (fromGroup)
        {
            var linkedUser = await Memory.Group.getUserObject(uid);
            return linkedUser?.GMT ?? 0;
        }
        
        var user = await Memory.User.GetUserAsync(uid);
        return user?.GMT ?? 0;
    }
}