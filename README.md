# SevenFridaysMusic
**Telegram-бот** для поиска и скачивания аудиофайлов с **YouTube** в одном приложении без использования сторонних сайтов

## Стек технологий
- **C# / .NET 9**: основной язык разработки
- **Telegram.Bot API**: интерфейс бота в Telegram
- **Dependency Injection**: для уменьшения зависимости между сервисами
- **YoutubeExplode**: библиотека для парсинга треков с YouTube
- **FFmpeg**: форматирование файлов от YouTubeExplode в аудио
- **Docker**: контейнеризация и развёртывание бота в изолированном окружении

## Запуск через Docker
1. Получите токен бота в Telegram у бота @BotFather
2. Клонируйте репозиторий и перейдите в папку проекта

```bash
git clone https://github.com/SeptemNet/SevenFridaysMusic.git
cd SevenFridaysMusic
```

3. Соберите Docker-образ

```bash
docker build -t seven-fridays-bot .
```

4. Запустите контейнер

```bash
docker run -d --name music_bot -e TELEGRAM_BOT_TOKEN="ТВОЙ_ТОКЕН" seven-fridays-bot
```