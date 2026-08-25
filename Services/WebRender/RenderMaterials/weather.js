function getRandomInt(min, max) {
    min = Math.ceil(min);
    max = Math.floor(max);
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

function runProd() {
    return; // telegram is not supporting transparent png files in chats YET
    let fixStyle = document.createElement('style')
    fixStyle.textContent = `html, body {background: transparent !important}`
    document.head.appendChild(fixStyle)
}

const WebRenderTypes  = {
    Background: 0,
    AddAfter: 1,
}
const WebRenderSelection  = {
	Linear: 0,
	Random: 1
}

let Additions = {
	imagesResponded: 0,
	GetImage: (OBJ, Default) => {
		if (OBJ === null || OBJ.ReadyContent === undefined || OBJ.ReadyContent.length === 0) { alert(-1); return Default }
		let content = OBJ.ReadyContent;
		if (Additions.imagesResponded >= content.length) {Additions.imagesResponded = 0;}
		let ret = content[Additions.imagesResponded];
		Additions.imagesResponded++;
		return ret
	}, 
	Get: (OBJ, Key, Default) => {
		if (OBJ === null || !Object.keys(OBJ).includes(Key)) return Default
		let content = OBJ[Key];
        if (content === undefined) return Default
        return content;	
	}
}

let weatherDiv = document.querySelectorAll('div.weather');

async function updateWeather(weatherList) {
    console.log(weatherList)
    let AddInfo = Object.keys(weatherList).includes("Additional") ? weatherList.Additional : {};
    document.body.classList.remove("list")
    weatherDiv.forEach(weather => {
        weather.innerHTML = ''
    });
    
    
    let addedBlocks = [];
    let i = 0;
    for (let weather of weatherList.Main) {
        i++;

        let isFirstBlock = i <= 2;
        let targetWeatherBlock = document.querySelector(`div.weather.${isFirstBlock ? 'first' : 'second'}`)
        await createBlock(targetWeatherBlock, weather, AddInfo, {forceDither: true, isDefaultBlock: true});
    }
    
    
    if (Additions.Get(AddInfo, "ShowType", 0) === WebRenderTypes.AddAfter) {
        let addList = Additions.Get(AddInfo, "Content", []);
        let AddMax = Additions.Get(AddInfo, "AddMax", 0);
        
        let i = 0;
        for (let Cont of addList) {
            i++;
            if (i > AddMax) continue;
            
            let uniqueID = `adNum_${i}`;
            let block = document.createElement("div");
            block.className = 'weather additional';
            block.id = uniqueID;
            document.body.appendChild(block);
            await createBlock(block, null, AddInfo, {isDefaultBlock: false});
            addedBlocks.push("div#"+uniqueID);
        }        
    }
    
    return addedBlocks;
}

async function createBlock(targetWeatherBlock, weather, AddInfo, data) {
    let forceDither = Object.keys(data).includes("forceDither") ? data.forceDither : false;
    let isDefaultBlock = Object.keys(data).includes("isDefaultBlock") ? data.isDefaultBlock : false;
    
    
    if (AddInfo === undefined || AddInfo === null) {AddInfo = {ShowDither: true}}
    let newWeatherDisplay = document.createElement('div');
    newWeatherDisplay.classList.add('display');

    let darkRatio = weather === null ? 101 : await getImageBrightness(`weather_icons/${weather.WeatherIcon}.svg`);
    let isDark = darkRatio<100;
    let darkAlignment = (isDark ? (100 - darkRatio)/100 : 0) * 2.5;

    let ranStyle = `filter: brightness(${ isDark
        ? 1+darkAlignment
        : 1+(getRandomInt(100, 250)/1000)}) `;

	
    let addBlocksAfter = Additions.Get(AddInfo, "ShowType", 0) === WebRenderTypes.AddAfter;
    let backgroundImageMode = Additions.Get(AddInfo, "ShowType", 0) === WebRenderTypes.Background;
    
    let defIcon = `weather_icons/${weather ? weather.WeatherIcon : null}.svg`;
	let bgImg = addBlocksAfter ?
        defIcon :
        Additions.GetImage(AddInfo, defIcon);
    
	let asDither = Additions.Get(AddInfo, "ShowDither", true) || (forceDither && !backgroundImageMode) ?
        "dithering " + (AddInfo.ShowType === WebRenderTypes.Background ? "big" : "")
        : "";
    
	let AutoFit = (AddInfo.AutoFit === true) ? "AutoFit" : "";
    
    
	let Brightness = Additions.Get(AddInfo, "Brightness", null);
	let Blur = Additions.Get(AddInfo, "Blur", null);
	let AllowHue = Additions.Get(AddInfo, "AllowHue", false);
    
    if ((Brightness !== null || Blur !== null || AllowHue) && !addBlocksAfter) {ranStyle = "filter: "; newWeatherDisplay.classList.add('forceStyles');}
    if (!isDefaultBlock && weather === null) {ranStyle = "filter: ";}
    
    if (Brightness !== null && (!isDefaultBlock || isDefaultBlock && !addBlocksAfter)) {ranStyle += ` brightness(${Brightness})`}
    if (Blur !== null && (!isDefaultBlock || isDefaultBlock && !addBlocksAfter)) {ranStyle += ` blur(${Blur}px)`}
    if (AllowHue && (!isDefaultBlock || isDefaultBlock && !addBlocksAfter)) {ranStyle += ` hue-rotate(${getRandomInt(-8, 8)}deg)`}


    if (weather) {
        newWeatherDisplay.innerHTML = `
            <div class="background ${AutoFit}" style="${ranStyle}">
                <img bg src="${bgImg}" alt="background" accent="${isDark ? 1 : getRandomInt(1, 4)}" class="${asDither}">
                <img visible src="weather_icons/${weather.WeatherIcon}.svg" alt="icon">
            </div>
           
            <div line1>
                <time> ${weather.Time} </time>
                <temp> ${Math.round(weather.Temp)}° </temp>
            </div>
            <div line2>
               <desc> ${weather.WeatherTitle} </desc>
            </div>
        `
    } else {
        newWeatherDisplay.innerHTML = `
            <img fullbg src="${Additions.GetImage(AddInfo, defIcon)}" alt="background" class="${asDither} asBig" style="${ranStyle}">
        `
    }
    targetWeatherBlock.appendChild(newWeatherDisplay);
}
function getImageBrightness(imgSrc, backgroundColor = '#111111ac') {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.crossOrigin = 'anonymous';
        img.src = imgSrc;

        img.onload = () => {
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');

            canvas.width = img.naturalWidth || 100;
            canvas.height = img.naturalHeight || 100;

            ctx.fillStyle = backgroundColor;
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            ctx.drawImage(img, 0, 0, canvas.width, canvas.height);

            let imageData;
            try {
                imageData = ctx.getImageData(0, 0, canvas.width, canvas.height).data;
            } catch (err) {
                reject(err);
                return;
            }

            let totalBrightness = 0;
            const pixelCount = imageData.length / 4;

            for (let i = 0; i < imageData.length; i += 4) {
                const r = imageData[i];
                const g = imageData[i + 1];
                const b = imageData[i + 2];
                totalBrightness += (r * 299 + g * 587 + b * 114) / 1000;
            }

            resolve(totalBrightness / pixelCount);
        };

        img.onerror = () => reject(new Error(`Failed to load image: ${imgSrc}`));
    });
}

async function testIcons() {
    document.querySelectorAll('div.weather').forEach(weather => {weather.innerHTML = '';});
    document.body.classList.add("list")
    let icons = ['blowing_snow.svg', 'clear_day.svg', 'clear_night.svg', 'clear_then_cloudy.svg', 'clear_then_rain.svg', 'clear_then_snow.svg', 'clear_with_cloudy.svg', 'clear_with_rain.svg', 'clear_with_snow.svg', 'cloudy.svg', 'cloudy_then_clear.svg', 'cloudy_then_rain.svg', 'cloudy_then_snow.svg', 'cloudy_with_clear.svg', 'cloudy_with_rain.svg', 'cloudy_with_snow.svg', 'drizzle.svg', 'flurries.svg', 'haze_fog.svg', 'heavy_rain.svg', 'heavy_snow.svg', 'hurricane.svg', 'icy.svg', 'mixed_rain_hail_sleet.svg', 'mostly_clear_night.svg', 'mostly_cloudy_day.svg', 'mostly_cloudy_night.svg', 'not_available.svg', 'partly_cloudy_day.svg', 'partly_cloudy_night.svg', 'rain_showers.svg', 'rain_then_clear.svg', 'rain_then_cloudy.svg', 'rain_then_snow.svg', 'rain_with_clear.svg', 'rain_with_cloudy.svg', 'rain_with_snow.svg', 'scattered_rain_showers_day.svg', 'scattered_rain_showers_night.svg', 'scattered_snow_showers_day.svg', 'scattered_snow_showers_night.svg', 'sleet_hail.svg', 'snow_then_clear.svg', 'snow_then_cloudy.svg', 'snow_then_rain.svg', 'snow_with_clear.svg', 'snow_with_cloudy.svg', 'snow_with_rain.svg', 'strong_thunderstorms.svg', 'thunderstorms.svg', 'thunderstorms_day.svg', 'thunderstorms_night.svg', 'tornado.svg', 'very_cold.svg', 'very_hot.svg', 'windy.svg', 'wintry_mix.svg'];
    for (let i = 0; i < icons.length; i++) {
        let block = {WeatherIcon: icons[i].replace('.svg', ''), Time: "09:00", WeatherTitle: "Title", Temp: "20"};
        let isFirstBlock = i % 2 === 0;
        let targetWeatherBlock = document.querySelector(`div.weather.${isFirstBlock ? 'first' : 'second'}`)
        
        
        await createBlock(targetWeatherBlock, block, {})
    }
}