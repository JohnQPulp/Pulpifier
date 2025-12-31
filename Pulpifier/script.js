const app = document.getElementById('app');
const params = new URLSearchParams(window.location.search);

let pos = 0;
const sVal = Number(localStorage.getItem(getLocalStorageKey('l')));
if (!Number.isNaN(sVal) && sVal !== 0) {
  pos = sVal;
}
const pVal = Number(params.get("p"));
if (!Number.isNaN(pVal) && pVal !== 0) {
  pos = Math.floor(pVal / 3);
}
const lVal = Number(params.get("l"));
if (!Number.isNaN(lVal) && lVal !== 0) {
  pos = lVal;
  const url = new URL(window.location);
  url.searchParams.delete("l");
  history.replaceState(null, "", url);
}
onPosUpdate(true);

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
    onPosUpdate();
  }
}
function prevPulp() {
  if (pos - 1 >= 0) {
    prependPulp(--pos - 1);
    app.removeChild(app.lastChild);
    onPosUpdate();
  }
}
document.addEventListener("keydown", function (e) {
  if (e.key === "ArrowRight") {
    nextPulp();
  } else if (e.key === "ArrowLeft") {
    prevPulp();
  }
});
document.addEventListener("click", function (e) {
  if (e.target.tagName === "IMG" || (e.target.tagName === "DIV" && (e.target.id === "back" || e.target.className === "characters"))) {
    if (e.clientX >= window.innerWidth / 2) {
      nextPulp();
    } else {
      prevPulp();
    }
  }
});
document.addEventListener("wheel", function (e) {
  if (e.shiftKey) {
    if (e.deltaY > 0) {
      nextPulp();
    } else {
      prevPulp();
    }
  }
});
function setPos(i, firstUpdate) {
  pos = i;
  app.innerHTML = buildPulp(i - 1) + buildPulp(i) + buildPulp(i + 1);
  onPosUpdate(firstUpdate);
}
window.addEventListener("load", e => {
  setPos(pos, true);
});
function onPosUpdate(firstUpdate) {
  localStorage.setItem(getLocalStorageKey("l"), pos);
  window["handlePosUpdate"] && window["handlePosUpdate"]();
  if (!firstUpdate) {
    app.scrollIntoView();
  }
}
function getLocalStorageKey(k) {
  const bookId = window['bookId'];
  if (bookId) {
    return bookId + ':' + k;
  }
  return k;
}