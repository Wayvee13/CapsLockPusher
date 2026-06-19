CapsLockPusher UI v2

Что нового:
- Красивое окно управления.
- Видно активен скрипт или остановлен.
- Кнопка Start / Stop.
- Кнопка Press now.
- Настройка интервала в секундах.
- Показ последнего нажатия и следующего нажатия.
- Прогресс-бар до следующего CapsLock.
- Иконка в трее.
- Красивая иконка приложения.

Как собрать EXE через GitHub:
1. Замени файлы в репозитории на файлы из этого архива.
2. Важно: .github/workflows/build-windows.yml тоже заменить.
3. Открой Actions → Build Windows EXE → Run workflow.
4. После успешной сборки скачай Artifact:
   CapsLockPusher-UI-win-x64

Как собрать локально:
1. Установи .NET SDK 8.
2. Запусти build.bat.
3. EXE будет здесь:
   bin\Release\net8.0-windows\win-x64\publish\CapsLockPusher.exe

Как пользоваться:
- Открыл EXE → скрипт сразу активен.
- Stop → остановить таймер.
- Start → запустить снова.
- Press now → нажать CapsLock вручную.
- Закрытие крестиком сворачивает приложение в трей.
- В трее ПКМ по иконке → Open window / Start Stop / Press now / Exit.
