function getRandomInt(min, max) {
    min = Math.ceil(min);
    max = Math.floor(max);
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

function runProd() {
    return; // telegram is not supporting transparent png files in chats
    console.log("Prod!");
    let fixStyle = document.createElement('style')
    fixStyle.textContent = `html, body {background: transparent !important}`
    document.head.appendChild(fixStyle)
}

function updateWeather(weatherList) {
    let i = 0;
    for (let weather of weatherList) {
        i++;
        
        let isFirstBlock = i <= 2;
        console.log(`div.weather.${isFirstBlock ? 'first' : 'second'}`);
        let targetWeatherBlock = document.querySelector(`div.weather.${isFirstBlock ? 'first' : 'second'}`)
        
        let newWeatherDisplay = document.createElement('div');
        newWeatherDisplay.classList.add('display');
        newWeatherDisplay.innerHTML = `
            <div class="background">
             <img bg src="weather_icons/${weather.WeatherIcon}.svg" alt="icon" accent="${getRandomInt(1, 4)}" class="dithering">
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

