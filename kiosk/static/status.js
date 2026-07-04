const tbody = document.querySelector("#vehicle-table tbody");

async function refresh() {
  const response = await fetch("/api/vehicles");
  const vehicles = await response.json();

  tbody.innerHTML = "";
  for (const v of vehicles) {
    const row = document.createElement("tr");
    row.innerHTML = `
      <td>${v.parking_spot}</td>
      <td>${v.license_plate}</td>
      <td>${v.vehicle_model || "-"}</td>
      <td>${v.battery_level}%</td>
      <td>${v.expected_departure_time}</td>
      <td>${v.created_at}</td>
      <td><button class="delete-btn" data-id="${v.id}">삭제</button></td>
    `;
    tbody.appendChild(row);
  }

  for (const btn of tbody.querySelectorAll(".delete-btn")) {
    btn.addEventListener("click", async () => {
      await fetch(`/api/vehicles/${btn.dataset.id}`, { method: "DELETE" });
      refresh();
    });
  }
}

refresh();
setInterval(refresh, 10000);
