CapsLockPusher

Что делает:
- Каждые 60 секунд отправляет нажатие CapsLock через Windows SendInput.
- Работает в фоне без окна.
- В трее появляется иконка.
- ПКМ по иконке в трее:
  - Press CapsLock now — нажать CapsLock сразу
  - Exit — закрыть программу

Как собрать EXE на Windows:
1. Установи .NET SDK 8:
   https://dotnet.microsoft.com/download
2. Распакуй этот проект.
3. Запусти build.bat
4. Готовый EXE будет здесь:
   bin\Release\net8.0-windows\win-x64\publish\CapsLockPusher.exe

Если нужен файл поменьше:
- Запусти build-small.bat
- Но тогда на ПК должен быть установлен .NET Desktop Runtime 8.

Важно:
- CapsLock реально переключает состояние CapsLock.
- Для остановки программы нажми ПКМ по иконке в трее → Exit.
- Если отчётный скрипт запущен от администратора, а CapsLockPusher нет, Windows может не дать отправить ввод в админ-окна. В таком случае запускай CapsLockPusher.exe от администратора.
