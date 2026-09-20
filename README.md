# Scheder
### Telegram bot, created to automatize schedule routine
Built for Top-Academy on Telegram.Bot (Api 10.3)

---

## Features:
- Token Pre-Fetch
- Context Activation
- Weather Support
- "I Am Late" Feature
- Date Watcher service
- Safe group binding
- Token caching
- Response caching
- Rich & Ephemeral messages support

---

## .env configuration:
`TG_TOKEN` - Telegram Token that Bot Uses
<br>

`DB_HOST` - Target PostgreSQL Host
<br>
`DEV_HOST` - PostgreSQL Host in dev mode
<br>
`DB_NAME` - Main PostgreSQL database name
<br>
`DB_USER` - Main PostgreSQL user
<br>
`DB_NAME` - Main PostgreSQL database name
<br>
`DB_PASS` - Main PostgreSQL password

<br>

#### Additional .env values:

`WEATHER_TOKEN` - **string** | [WeatherAPI](https://weatherapi.com) api key. If not set - weather will not be available. City will be parsed automatically
<br>
`SkipChromium` - **bool** | Recommended if `WEATHER_TOKEN` is not set. Do not start Chromium at all.
<br>
`DebugUID` - **long** | Enables metric for specific ID | <i> `ProdPrepare` requires a value here</i>
<br>
`FastStart` - **bool** | Loads additional things in a background, use for dev purposes only!
<br>
`UseProxy` - **bool** | Set to true or false and set `ProxyConnection`
<br>
`UseProxyForWeather` - **bool** | Recommended if bot is hosted in Russia. Requires `ProxyConnection`
<br>
`ProxyConnection` - **str** | Use one line configuration. Example: `socks5://password:password@ip_here:port_here`. Supports: HTTP(s), Socks4/4a/5/5e
<br>
`TalkativePerformance` - **bool** | Allow to show metric not only to DebugUID
<br>
`DisableCaching` - **bool** | Disables caching system
<br>
`DisableTokenCaching` - **bool** | If true - user token will not be cached
<br>
`DisableEarlyDayFix` - **bool** | Disables protection for tomorrow dates at early hours. See "Details"
<br>

`TimeMissAPI`, `PreFetchData`, `WeatherSpec` - See "Details"


<details>
    <summary>More Details</summary>
    <br>


### TimeMissApi
<details>

### User can notify teacher that he will be late. This requires an HTTPS endpoint chained with OmniTools (1.7.0+):
`TimeMissAPI` sets main endpoint, for example: **https://journalui.ru/api/late**

When user hits "I will be late for 15 minutes" bot sends a POST `/update`, so server is getting:
`https://journalui.ru/api/late/update` with this data:
```
{
    "groupId": int,
    "tgId": long,
    "cityId": int,
    "studentId": long,
    "minutesLate": string,
    "userName": string,
    "userTime": string,
    "targetDate": string,
    "action": "update"
}
```

<br>

To delete this note from server, bot sends a POST `/update` with this data:
```
{
    "groupId": int,
    "tgId": long,
    "cityId": int,
    "action": "delete"
}
```

If `TimeMissApi` not set - this feature will not be available
</details>

---

### PreFetchData
<details>

### Specify token updates for specific IDs.
Bot will be refreshing token for specified IDs. You can use it creating a json with information:
```
{
    "Id": long,
    "ParseHourList": [
        "00": 0,
        "10": 1,
        "11": 1,
        "12": 2,
        "13": 3
    ],
    "IsGroup": bool,
    "CopyFor": [
        {"Id": long, "IsGroup": bool}
    ]
}
```
This config will parse and cache token in this conditions:
- At 00:00 it will not parse any token
- At 10:00 and 11:00 will parse and cache token once
- At 12:00 will parse and cache token twice (at 12:00 and 12:30)
- At 13:00 will parse and cache token three times (at 12:00, 12:20, 12:40)

`IsGroup` - required. Defines, that target ID is (not) group  
`CopyFor` - *optional*. Defines, copy cached token for another groups and users

> [!WARNING]
> There’s no point in updating the token more often than 3 times an hour. Journal stores the token permissions for ~25–30 minutes.


### Why does this thing exists?
This thing is preparing token before users will ask bot for any action. This mechanism allows to use ready token instead of parsing fresh one. In brief - reduces token parse time

</details>

---

### WeatherSpec
<details>

### Allows to modify background in weather summary
Config example:

```
[
    {
        "TargetDates": ["19.11.2026", "20.11.2026", "21.11.2026", "22.11.2026"],
        "ShowType": 0,
        "Brightness": 0.75,
        "ShowDither": true,
        "AllowHue": false,
        "isContentInUrl": true,
        "Content":
            [ "https://www.rockstargames.com/VI/_next/static/media/Jason_and_Lucia_01_landscape.12x2gvspcm_3m.jpg",
              "https://www.rockstargames.com/VI/_next/static/media/Jason_and_Lucia_02_landscape.0cosv-uzbpt91.jpg",
              "https://www.rockstargames.com/VI/_next/static/media/Brian_Heder_landscape.0a-egj5b8yo1q.jpg",
              "https://www.rockstargames.com/VI/_next/static/media/Cal_Hampton_landscape.17k7bnt3myg.2.jpg"
            ],
        "Selection": 1
    }
]
```

In this example:

`TargetDates` - **List\<str\>** | Specifies rule for specific days
<br>
`ShowType` - **int** | Display Type. `0 - Show on background` | `1 - Show after weather blocks`
<br>
`Brightness` - **double** | Image brightness (0.75 Default)
<br>
`ShowDither` - **bool** | (Only if `ShowType=0`). If disabled - dithering effect will not be applied
<br>
`AutoFit` - **bool** | Automatically adjust images to fit the specified dimensions
<br>
`AllowHue` - **bool** | Slightly modify the image to get different shades
<br>
`isContentInUrl` - **bool** | Requires if content is URL
<br>
`Content` - **List\<str\>** | List of content to display. If `isContentInUrl=false`, specify the path to the image.
<br>
`Selection` - **int** | Random mixing and unique content selection for each block `0 = Disabled`, `1 = Enabled`
<br>
`Blur` - **double** | Apply blur effect (in px) for each image
<br>
`AddMax` - **int** | Applies only for `ShowType=1`. Sets maximum amount of additional content blocks to show. This is useful if you only need one piece of content from the list of contents. In the configuration `Selection=1` and `AddMax=1`, one block with random content will be added.

You can create several rules with different settings.

</details>
</details>
