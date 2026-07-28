function getRandomInt(min, max) {
    min = Math.ceil(min);
    max = Math.floor(max);
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

function runProd() {
    return; // telegram is not supporting transparent png files in chats YET
    console.log("Prod!");
    let fixStyle = document.createElement('style')
    fixStyle.textContent = `html, body {background: transparent !important}`
    document.head.appendChild(fixStyle)
}

let weatherDiv = document.querySelectorAll('div.weather');

async function updateWeather(weatherList) {
    weatherDiv.forEach(weather => {weather.innerHTML=''});
    let i = 0;
    for (let weather of weatherList) {
        i++;
        
        let isFirstBlock = i <= 2;
        let targetWeatherBlock = document.querySelector(`div.weather.${isFirstBlock ? 'first' : 'second'}`)
        
        let newWeatherDisplay = document.createElement('div');
        newWeatherDisplay.classList.add('display');

        let darkRatio = await getImageBrightness(`weather_icons/${weather.WeatherIcon}.svg`);
        let isDark = darkRatio<100;
        let darkAlignment = (isDark ? (100 - darkRatio)/100 : 0) * 2.1;
        
        let ranStyle = `filter: brightness(${ isDark
                 ? 1+darkAlignment 
                 : 1+(getRandomInt(100, 250)/1000)}); `;
        
        newWeatherDisplay.innerHTML = `
            <div class="background" style="${ranStyle}">
             <img bg src="weather_icons/${weather.WeatherIcon}.svg" alt="background" accent="${isDark ? 1 : getRandomInt(1, 4)}" class="dithering">
            </div>
           
            <img visible src="weather_icons/${weather.WeatherIcon}.svg" alt="icon">
            <div line1>
                <time> ${weather.Time} </time>
                <temp> ${Math.round(weather.Temp)}° </temp>
            </div>
            <div line2>
               <desc> ${weather.WeatherTitle} </desc>
            </div>
        `
        targetWeatherBlock.appendChild(newWeatherDisplay);
    }
    
    console.log(weatherList);
}


function getImageBrightness(imgSrc, backgroundColor = '#11111180') {
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