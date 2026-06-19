use chrono::Local;
use serde::Serialize;
use std::{
    sync::{Arc, Mutex},
    thread,
    time::{Duration, Instant},
};
use tauri::{
    menu::{Menu, MenuItem},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    Manager,
};

#[derive(Clone)]
struct AppStateInner {
    running: bool,
    interval: u64,
    last_press: Option<String>,
    next_press_at: Instant,
}

type SharedState = Arc<Mutex<AppStateInner>>;

#[derive(Serialize)]
struct UiState {
    running: bool,
    interval: u64,
    last_press: Option<String>,
    seconds_left: u64,
}

#[tauri::command]
fn get_state(state: tauri::State<SharedState>) -> UiState {
    let s = state.lock().unwrap();
    let seconds_left = if s.running {
        s.next_press_at.saturating_duration_since(Instant::now()).as_secs()
    } else {
        0
    };

    UiState {
        running: s.running,
        interval: s.interval,
        last_press: s.last_press.clone(),
        seconds_left,
    }
}

#[tauri::command]
fn start_timer(state: tauri::State<SharedState>) {
    let mut s = state.lock().unwrap();
    s.running = true;
    s.next_press_at = Instant::now() + Duration::from_secs(s.interval);
}

#[tauri::command]
fn stop_timer(state: tauri::State<SharedState>) {
    let mut s = state.lock().unwrap();
    s.running = false;
}

#[tauri::command]
fn set_interval(seconds: u64, state: tauri::State<SharedState>) {
    let mut s = state.lock().unwrap();
    let safe_seconds = seconds.clamp(5, 3600);
    s.interval = safe_seconds;
    s.next_press_at = Instant::now() + Duration::from_secs(safe_seconds);
}

#[tauri::command]
fn press_now(state: tauri::State<SharedState>) {
    press_caps_lock();

    let mut s = state.lock().unwrap();
    s.last_press = Some(Local::now().format("%H:%M:%S").to_string());
    s.next_press_at = Instant::now() + Duration::from_secs(s.interval);
}

pub fn run() {
    let state: SharedState = Arc::new(Mutex::new(AppStateInner {
        running: true,
        interval: 60,
        last_press: None,
        next_press_at: Instant::now() + Duration::from_secs(60),
    }));

    let worker_state = state.clone();
    thread::spawn(move || loop {
        thread::sleep(Duration::from_millis(250));

        let should_press = {
            let s = worker_state.lock().unwrap();
            s.running && Instant::now() >= s.next_press_at
        };

        if should_press {
            press_caps_lock();

            let mut s = worker_state.lock().unwrap();
            s.last_press = Some(Local::now().format("%H:%M:%S").to_string());
            s.next_press_at = Instant::now() + Duration::from_secs(s.interval);
        }
    });

    tauri::Builder::default()
        .manage(state)
        .setup(|app| {
            let show = MenuItem::with_id(app, "show", "Open window", true, None::<&str>)?;
            let toggle = MenuItem::with_id(app, "toggle", "Start / Stop", true, None::<&str>)?;
            let press = MenuItem::with_id(app, "press", "Press CapsLock now", true, None::<&str>)?;
            let quit = MenuItem::with_id(app, "quit", "Exit", true, None::<&str>)?;
            let menu = Menu::with_items(app, &[&show, &toggle, &press, &quit])?;

            let app_handle = app.handle().clone();

            TrayIconBuilder::new()
                .icon(app.default_window_icon().unwrap().clone())
                .menu(&menu)
                .tooltip("CapsLock Pusher")
                .on_menu_event(move |app, event| match event.id.as_ref() {
                    "show" => {
                        if let Some(window) = app.get_webview_window("main") {
                            let _ = window.show();
                            let _ = window.set_focus();
                        }
                    }
                    "toggle" => {
                        let state = app.state::<SharedState>();
                        let mut s = state.lock().unwrap();
                        s.running = !s.running;
                        s.next_press_at = Instant::now() + Duration::from_secs(s.interval);
                    }
                    "press" => {
                        let state = app.state::<SharedState>();
                        press_caps_lock();
                        let mut s = state.lock().unwrap();
                        s.last_press = Some(Local::now().format("%H:%M:%S").to_string());
                        s.next_press_at = Instant::now() + Duration::from_secs(s.interval);
                    }
                    "quit" => {
                        app.exit(0);
                    }
                    _ => {}
                })
                .on_tray_icon_event(move |_tray, event| {
                    if let TrayIconEvent::Click {
                        button: MouseButton::Left,
                        button_state: MouseButtonState::Up,
                        ..
                    } = event
                    {
                        if let Some(window) = app_handle.get_webview_window("main") {
                            let _ = window.show();
                            let _ = window.set_focus();
                        }
                    }
                })
                .build(app)?;

            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            get_state,
            start_timer,
            stop_timer,
            set_interval,
            press_now
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri app");
}

#[cfg(target_os = "windows")]
fn press_caps_lock() {
    use windows::Win32::UI::Input::KeyboardAndMouse::{
        SendInput, INPUT, INPUT_0, INPUT_KEYBOARD, KEYBDINPUT, KEYBD_EVENT_FLAGS, KEYEVENTF_KEYUP,
        VIRTUAL_KEY, VK_CAPITAL,
    };

    unsafe {
        let inputs = [
            INPUT {
                r#type: INPUT_KEYBOARD,
                Anonymous: INPUT_0 {
                    ki: KEYBDINPUT {
                        wVk: VIRTUAL_KEY(VK_CAPITAL.0),
                        wScan: 0,
                        dwFlags: KEYBD_EVENT_FLAGS(0),
                        time: 0,
                        dwExtraInfo: 0,
                    },
                },
            },
            INPUT {
                r#type: INPUT_KEYBOARD,
                Anonymous: INPUT_0 {
                    ki: KEYBDINPUT {
                        wVk: VIRTUAL_KEY(VK_CAPITAL.0),
                        wScan: 0,
                        dwFlags: KEYEVENTF_KEYUP,
                        time: 0,
                        dwExtraInfo: 0,
                    },
                },
            },
        ];

        let _ = SendInput(&inputs, std::mem::size_of::<INPUT>() as i32);
    }
}

#[cfg(not(target_os = "windows"))]
fn press_caps_lock() {}
