const app = document.getElementById('app');
const params = new URLSearchParams(window.location.search);
let pos = Number(params.get("p"));
if (Number.isNaN(pos)) pos = 0;
pos = Math.floor(pos / 3);
function buildPulp(i) {
  if (i < 0 || i >= htmlArr.length) {
    return `<div id='pulp'></div>`;
  }
  var background = backgrounds[backgroundIds[i]].split(';');
  return (
`<div id='pulp'>
  <div id='back' style='background-image: url("images/b-${background[0]}.webp");${background.length === 1 ? "" : ("filter:" + background[1])}'></div>
  ${imageHtmls[i]}
  <div id='foot'>
    <div>${speakers[i] === "" ? "" : ("<div class='speaker-back' style='background-image: url(images/" + speakers[i] + ")'></div>")}</div>
    <div id='text'>${htmlArr[i]}</div>
    <div></div>
  </div>
</div>`);
}
function appendPulp(i) {
  app.innerHTML += buildPulp(i);
}
function prependPulp(i) {
  app.innerHTML = buildPulp(i) + app.innerHTML;
}
function nextPulp() {
  if (pos + 1 < htmlArr.length) {
    app.removeChild(app.firstChild);
    appendPulp(++pos + 1);
  }
}
function prevPulp() {
  if (pos - 1 >= 0) {
    prependPulp(--pos - 1);
    app.removeChild(app.lastChild);
  }
}
document.addEventListener("keydown", function (e) {
  if (e.key === " " || e.key === "Spacebar" || e.key === "ArrowRight") {
    nextPulp();
  } else if (e.key === "ArrowLeft") {
    prevPulp();
  }
});
document.addEventListener("click", function (e) {
  if (e.target.tagName === "IMG" || (e.target.tagName === "DIV" && e.target.id === "pulp")) {
    nextPulp();
  }
});
document.addEventListener("wheel", function (e) {
  if (e.deltaY > 0) {
    nextPulp();
  } else {
    prevPulp();
  }
});
window.addEventListener("load", e => {
  appendPulp(pos - 1);
  appendPulp(pos);
  appendPulp(pos + 1);
});