using System.Text.Json.Nodes;
using Scheder.Services.Database;
using Scheder.Services.JournalAPI;
using Scheder.Tools;

namespace Scheder.Services.iWillBeLate;

public class TimeMissApi {

    
    /* bool isGroup */ // Group cant ask

    private static async Task<JsonNode?> GetAuth(AuthClass auth) {
        var authResult = await API.GetAuthAsync(
            auth.Login,
            auth.Password
        );
        
        return authResult.Item1;
    }


    public static async Task<DirectTimeMissApi.MissedTimeSendResult> Update(
        long tgId,
        string minutesLate
    ) {

        var auth = await Memory.User.GetAuthAsync(tgId);
        if (auth == null) {
            return ApiCodesTranslator.UpdateDescription(null);
        }

        var authData = await GetAuth(auth);
        if (authData?["access_token"] is null) return ApiCodesTranslator.UpdateDescription(null);
        var token = authData["access_token"]!.ToString();

        var response = await API.GetAsync(
            "https://msapi.top-academy.ru/api/v2/settings/user-info",
            authToken: token
        );


        var userInfo = JsonNode.Parse(await response.Content.ReadAsStringAsync());

        var groupId = long.Parse(userInfo?["current_group_id"]?.ToString()!);
        var cityId = int.Parse(authData["city_data"]?["id_city"]?.ToString()!);
        var userName = userInfo?["full_name"]?.ToString()!;
        var studentId = long.Parse(userInfo?["student_id"]?.ToString()!);

        var userTime = await GmtTool.GetCurrentDatetimeWithGmt(tgId);
        var localUserTime = userTime.ToString("dd.MM.yyyy HH:mm");
        Console.WriteLine("localUserTime: " + localUserTime);
        var localUserDate = userTime.ToString("dd.MM.yyyy");


        var pushResult = await DirectTimeMissApi.Update(
            groupId,
            tgId,
            cityId,
            studentId,
            minutesLate,
            userName,
            localUserTime,
            localUserDate
        );

        return ApiCodesTranslator.UpdateDescription(pushResult);
    }




    public static async Task<DirectTimeMissApi.MissedTimeSendResult> Delete(
        long tgId
    ) {

        var auth = await Memory.User.GetAuthAsync(tgId);
        if (auth == null) {
            return ApiCodesTranslator.UpdateDescription(null);
        }
        
        var authData = await GetAuth(auth);
        if (authData?["access_token"] is null) return ApiCodesTranslator.UpdateDescription(null);
        
        var token = authData["access_token"]!.ToString();
        
        var response = await API.GetAsync(
            "https://msapi.top-academy.ru/api/v2/settings/user-info",
            authToken: token
        );
        
        
        var userInfo = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        
        var groupId = long.Parse(userInfo?["current_group_id"]?.ToString()!);
        var cityId = int.Parse(authData["city_data"]?["id_city"]?.ToString()!);


        var pushResult = await DirectTimeMissApi.Delete(
            groupId,
            tgId,
            cityId
        );
        
        return ApiCodesTranslator.UpdateDescription(pushResult);
        

    }


}