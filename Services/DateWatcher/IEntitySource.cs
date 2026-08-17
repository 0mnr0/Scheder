using System.Xml;
using Scheder.Services.Database;

namespace Scheder.Services.DateWatcher;

public interface IEntitySource
{
    Task<List<Memory.DayListener>> GetDayListeners(long uid);
    Task AddDayListener(long uid, string date, int? threadId);
    Task RemoveDayListener(long uid, string date);
    Task RemoveDayListener(long uid, Memory.DayListener date);
    Task ClearDayListeners(long uid);
    Task UpdateDayListener(long uid, string date, string newHash);
    Task UpdateDayListener(long uid, Memory.DayListener listener);
}

public class GroupSource : IEntitySource
{
    public Task<List<Memory.DayListener>> GetDayListeners(long uid) =>
        Memory.Group.GetDayListeners(uid);

    public Task AddDayListener(long uid, string date, int? threadId) =>
        Memory.Group.AddDayListener(uid, date, threadId);
    
    public Task RemoveDayListener(long uid, string date) =>
        Memory.Group.RemoveDayListener(uid, date);
    
    public Task RemoveDayListener(long uid, Memory.DayListener date) =>
        Memory.Group.RemoveDayListener(uid, date);
    
    public Task ClearDayListeners(long uid) =>
        Memory.Group.ClearDayListeners(uid);
    
    public Task UpdateDayListener(long uid, string date, string newHash) =>
        Memory.Group.UpdateDayListener(uid, date, newHash);
    
    public Task UpdateDayListener(long uid, Memory.DayListener listener) =>
        Memory.Group.UpdateDayListener(uid, listener);

}

public class UserSource : IEntitySource {
    public Task<List<Memory.DayListener>> GetDayListeners(long uid)
        => Memory.User.GetDayListeners(uid);
    
    public Task AddDayListener(long uid, string date, int? threadId) => 
        Memory.User.AddDayListener(uid, date, threadId);

    public Task RemoveDayListener(long uid, string date) =>
        Memory.User.RemoveDayListener(uid, date);
    
    public Task RemoveDayListener(long uid, Memory.DayListener date) =>
        Memory.User.RemoveDayListener(uid, date);
    
    public Task ClearDayListeners(long uid) =>
        Memory.User.ClearDayListeners(uid);
    
    public Task UpdateDayListener(long uid, string date, string newHash) =>
        Memory.User.UpdateDayListener(uid, date, newHash);
    
    public Task UpdateDayListener(long uid, Memory.DayListener listener) =>
        Memory.User.UpdateDayListener(uid, listener);
}