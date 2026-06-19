CapsLockPusher Tauri

Что это:
- Tauri-приложение с красивым HTML/CSS интерфейсом.
- Каждые N секунд нажимает CapsLock через Windows SendInput.
- Есть Start / Stop, Press now, статус, прогресс, трей.

Как собрать через GitHub:
1. Создай/очисти репозиторий.
2. Загрузи все файлы из этого архива в корень репозитория.
3. Проверь, что workflow лежит здесь:
   .github/workflows/build-tauri-windows.yml
4. Открой Actions.
5. Выбери Build Tauri Windows EXE.
6. Run workflow.
7. После зелёной сборки скачай Artifact:
   CapsLockPusher-Tauri-win-x64

Как собрать локально:
1. Установи Node.js LTS.
2. Установи Rust: https://rustup.rs/
3. В папке проекта:
   npm install
   npm run tauri build

Важно:
- Если отчётная программа запущена от администратора, запускай CapsLockPusher тоже от администратора.
- CapsLock реально переключает состояние клавиши.
