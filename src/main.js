const { invoke } = window.__TAURI__.core;

const $ = (id) => document.getElementById(id);

let state = {
  running: true,
  interval: 60,
  last_press: null,
  seconds_left: 60,
};

function formatTime(value) {
  if (!value) return "—";
  return value;
}

function render() {
  const running = state.running;

  $("badge").textContent = running ? "ACTIVE" : "STOPPED";
  $("badge").className = running ? "badge" : "badge stopped";

  $("statusText").textContent = running ? "Активен" : "Остановлен";

  $("toggleBtn").textContent = running ? "Stop" : "Start";
  $("toggleBtn").className = running ? "danger" : "danger success";

  $("lastPress").textContent = formatTime(state.last_press);
  $("nextPress").textContent = running ? `${state.seconds_left}s` : "Paused";
  $("footerText").textContent = `CapsLock будет нажат каждые ${state.interval} секунд`;

  const total = Math.max(1, state.interval);
  const left = Math.max(0, Math.min(total, state.seconds_left));
  const done = running ? Math.round((1 - left / total) * 100) : 0;
  $("progressBar").style.width = `${done}%`;

  if (String($("intervalInput").value) !== String(state.interval)) {
    $("intervalInput").value = state.interval;
  }
}

async function refresh() {
  state = await invoke("get_state");
  render();
}

$("toggleBtn").addEventListener("click", async () => {
  if (state.running) {
    await invoke("stop_timer");
  } else {
    await invoke("start_timer");
  }
  await refresh();
});

$("pressNowBtn").addEventListener("click", async () => {
  await invoke("press_now");
  await refresh();
});

$("intervalInput").addEventListener("change", async () => {
  const seconds = Math.max(5, Math.min(3600, Number($("intervalInput").value || 60)));
  await invoke("set_interval", { seconds });
  await refresh();
});

setInterval(refresh, 500);
refresh();
