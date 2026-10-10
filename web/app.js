(() => {
  'use strict';
  const root = document.getElementById('myday-example');
  const find = selector => root.querySelector(selector);
  const all = selector => root.querySelectorAll(selector);
  const date = find('input[type=date]');
  const list = find('.md-blocks');
  const picker = find('.md-picker');
  const status = find('.md-live');
  const key = 'myday.web.v1';
  const themes = [['#fff8f3','#f3e6df'],['#f0f5ef','#e1eadd'],['#f3f0fa','#e9e2f4']];
  const labels = {text:'오늘은 어떤 하루였나요?',todo:'해야 할 일',habit:'오늘 실천할 습관',emotion:'내 마음 기록'};
  let entries = {};
  let writable = true;
  let readable = true;
  let saved = true;
  let activeDate;
  let calendarMonth;
  function dayKey(day) {
    return `${day.getFullYear()}-${String(day.getMonth()+1).padStart(2,'0')}-${String(day.getDate()).padStart(2,'0')}`;
  }
  function validDate(value) {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
    const day = new Date(value+'T12:00:00');
    return !Number.isNaN(day.getTime()) && dayKey(day) === value;
  }
  function validate(value) {
    if (!value || typeof value !== 'object' || Array.isArray(value)) throw Error('기록 형식이 올바르지 않습니다.');
    for (const [day, entry] of Object.entries(value)) {
      if (!validDate(day) || !entry || !Number.isInteger(entry.theme) || entry.theme < 0 || entry.theme > 2 ||
          !['🐰','🐱','🐻'].includes(entry.character) || !Array.isArray(entry.blocks)) throw Error('날짜 또는 테마 형식이 올바르지 않습니다.');
      const ids = new Set();
      for (const block of entry.blocks) {
        if (!block || typeof block.id !== 'string' || ids.has(block.id) || !Object.hasOwn(labels,block.type) ||
            typeof block.text !== 'string' || typeof block.checked !== 'boolean') throw Error('블록 형식이 올바르지 않습니다.');
        ids.add(block.id);
      }
    }
    return value;
  }
  const current = () => entries[activeDate] || {theme:0,character:'🐰',blocks:[]};
  function updateControls() {
    all('[data-day], input[type=date], #calendar-days button, #today, #month-prev, #month-next').forEach(el => { el.disabled = !saved || !writable; });
    find('#retry').hidden = saved || !writable;
    find('.md-caption').textContent = !writable ? '읽기 실패 · 백업 복원으로 복구할 수 있습니다' : saved ? '이 브라우저에 저장됨 · 자동 저장' : '저장 실패 · 백업을 다운로드하거나 다시 저장해 주세요';
  }
  function save() {
    if (!writable) return false;
    try {
      localStorage.setItem(key, JSON.stringify({version:1, entries}));
      saved = true;
      status.textContent = '';
    } catch (error) {
      saved = false;
      status.textContent = '저장 공간 또는 브라우저 설정을 확인해 주세요. 현재 내용은 백업 다운로드로 보관할 수 있습니다.';
    }
    updateControls();
    updateOverview();
    return saved;
  }
  function edit(action) {
    if (!writable) return;
    const entry = current();
    entries[activeDate] = entry;
    action(entry);
    save();
  }
  function element(tag, text, className) {
    const el = document.createElement(tag);
    if (text !== undefined) el.textContent = text;
    if (className) el.className = className;
    return el;
  }
  function updateOverview() {
    const entry = current();
    find('#block-count').textContent = `${entry.blocks.length}개의 기록`;
    find('#friend-character').textContent = entry.character;
    for (const type of ['todo', 'habit']) {
      const blocks = entry.blocks.filter(b => b.type === type);
      const completed = blocks.filter(b => b.checked).length;
      find(`#${type}-count`).textContent = `${completed} / ${blocks.length}`;
      find(`#${type}-progress`).max = Math.max(1, blocks.length);
      find(`#${type}-progress`).value = completed;
    }
    const selected = new Date(activeDate+'T12:00:00');
    find('#date-heading').textContent = new Intl.DateTimeFormat('ko-KR', {year:'numeric',month:'long',day:'numeric',weekday:'long'}).format(selected);
    renderCalendar();
  }
  function renderCalendar() {
    const year = calendarMonth.getFullYear();
    const month = calendarMonth.getMonth();
    find('#calendar-month').textContent = `${year}년 ${month+1}월`;
    const days = find('#calendar-days');
    days.replaceChildren();
    for (let i=0; i<new Date(year,month,1).getDay(); i++) days.append(element('span'));
    const count = new Date(year,month+1,0).getDate();
    for (let day=1; day<=count; day++) {
      const value = dayKey(new Date(year,month,day));
      const button = element('button',String(day));
      button.type = 'button';
      button.setAttribute('aria-label', `${year}년 ${month+1}월 ${day}일${entries[value]?.blocks.length ? ', 기록 있음' : ''}`);
      button.setAttribute('aria-pressed',String(value === activeDate));
      if (value === dayKey(new Date())) button.setAttribute('aria-current','date');
      if (entries[value]?.blocks.length) button.classList.add('has-record');
      button.disabled = !saved || !writable;
      button.addEventListener('click', () => select(value));
      days.append(button);
    }
  }
  function render() {
    const entry = current();
    const phone = find('.md-phone');
    phone.style.setProperty('--md-bg', themes[entry.theme][0]);
    phone.style.setProperty('--md-soft', themes[entry.theme][1]);
    all('[data-theme]').forEach(b => b.setAttribute('aria-pressed', String(Number(b.dataset.theme) === entry.theme)));
    all('[data-character]').forEach(b => b.setAttribute('aria-pressed', String(b.dataset.character === entry.character)));
    find('[data-add=emotion]').textContent = entry.character+' 감정처리반';
    list.replaceChildren();
    const titles = {text:'✍ 오늘의 일기',todo:'☑ 할 일',habit:'🌱 습관 체크',emotion:entry.character+' 감정처리반'};
    entry.blocks.forEach((block,index) => {
      const card = element('section',undefined,'md-block');
      card.classList.toggle('is-complete', block.checked);
      card.append(element('h2',titles[block.type]));
      if (block.type === 'emotion') card.append(element('p','판단 없이 들어줄게. 지금 마음을 들려줘.','md-emotion'));
      const field = element('label',undefined,'md-field');
      const input = element('textarea');
      input.rows = ['text','emotion'].includes(block.type) ? 3 : 1;
      input.value = block.text;
      input.placeholder = block.type === 'emotion' ? '지금 떠오르는 마음을 편하게 적어 보세요…' : '작은 이야기부터 시작해 볼까요…';
      input.addEventListener('input', () => edit(() => { block.text = input.value; }));
      field.append(element('span',labels[block.type]),input);
      card.append(field);
      if (['todo','habit'].includes(block.type)) {
        const checkLabel = element('label',undefined,'md-check');
        const check = element('input'); check.type = 'checkbox'; check.checked = block.checked;
        const text = element('span',block.checked ? '오늘 완료했어요' : '완료하면 체크해 주세요');
        check.addEventListener('change', () => {
          edit(() => { block.checked = check.checked; });
          text.textContent = block.checked ? '오늘 완료했어요' : '완료하면 체크해 주세요';
          card.classList.toggle('is-complete', block.checked);
        });
        checkLabel.append(check,text); card.append(checkLabel);
      }
      const actions = element('div',undefined,'md-actions');
      [['↑ 위로',-1],['↓ 아래로',1],['삭제',0]].forEach(([title,delta]) => {
        const button = element('button',title); button.type = 'button';
        button.disabled = delta !== 0 && (index+delta < 0 || index+delta >= entry.blocks.length);
        button.addEventListener('click', () => {
          if (!delta && !window.confirm('이 블록과 작성한 내용을 삭제할까요?')) return;
          edit(e => {
            const removed = e.blocks.splice(index,1)[0];
            if (delta) e.blocks.splice(index+delta,0,removed);
          });
          render();
        });
        actions.append(button);
      });
      card.append(actions); list.append(card);
    });
    if (!entry.blocks.length) {
      const empty = element('section',undefined,'empty-state');
      empty.append(element('div','✳','empty-flower'),element('h2','아직 쓰이지 않은, 나의 하루'),element('p','기억하고 싶은 순간, 해내고 싶은 작은 일.\n아래에서 첫 번째 하루 조각을 골라 보세요.'));
      list.append(empty);
    }
    all('[data-theme], [data-character], .md-add').forEach(el => { el.disabled = !writable; });
    updateControls();
    updateOverview();
  }
  activeDate = dayKey(new Date()); date.value = activeDate;
  calendarMonth = new Date(new Date().getFullYear(),new Date().getMonth(),1);
  try {
    const raw = localStorage.getItem(key);
    if (raw) {
      const data = JSON.parse(raw);
      if (data.version !== 1) throw Error('지원하지 않는 저장 형식');
      entries = validate(data.entries);
    }
  } catch (error) {
    writable = false; readable = false;
    status.textContent = '기록을 읽지 못했습니다. 기존 저장 내용을 덮어쓰지 않습니다. 브라우저 저장 권한을 확인하거나 정상 백업을 복원해 주세요.';
  }
  all('[data-theme]').forEach(b => b.addEventListener('click', () => { edit(e => {e.theme = Number(b.dataset.theme);}); render(); }));
  all('[data-character]').forEach(b => b.addEventListener('click', () => { edit(e => {e.character = b.dataset.character;}); render(); }));
  find('.md-add').addEventListener('click', () => {
    picker.hidden = !picker.hidden;
    find('.md-add').setAttribute('aria-expanded',String(!picker.hidden));
    if (!picker.hidden) find('[data-add]').focus();
  });
  all('[data-add]').forEach(b => b.addEventListener('click', () => {
    edit(e => {e.blocks.push({id:crypto.randomUUID(),type:b.dataset.add,text:'',checked:false});});
    picker.hidden = true; find('.md-add').setAttribute('aria-expanded','false'); render();
    list.lastElementChild?.querySelector('textarea')?.focus();
  }));
  function select(next) {
    if (!validDate(next) || !saved || !writable) {date.value = activeDate; return;}
    activeDate = next; date.value = next; picker.hidden = true;
    find('.md-add').setAttribute('aria-expanded','false');
    const selected = new Date(next+'T12:00:00');
    calendarMonth = new Date(selected.getFullYear(),selected.getMonth(),1);
    render();
  }
  find('#today').addEventListener('click', () => select(dayKey(new Date())));
  for (const [id,offset] of [['#month-prev',-1],['#month-next',1]]) {
    find(id).addEventListener('click', () => {
      calendarMonth = new Date(calendarMonth.getFullYear(),calendarMonth.getMonth()+offset,1);
      renderCalendar();
    });
  }
  date.addEventListener('change', () => select(date.value));
  all('[data-day]').forEach(b => b.addEventListener('click', () => {
    const day = new Date(activeDate+'T12:00:00'); day.setDate(day.getDate()+Number(b.dataset.day)); select(dayKey(day));
  }));
  find('#retry').addEventListener('click',save);
  find('#export').addEventListener('click', () => {
    if (!readable) {status.textContent = '기록을 읽지 못해 백업할 수 없습니다. 정상 백업을 복원해 주세요.'; return;}
    const url = URL.createObjectURL(new Blob([JSON.stringify({version:1,entries},null,2)],{type:'application/json'}));
    const link = element('a'); link.href = url; link.download = 'myday-backup-'+dayKey(new Date())+'.json'; link.click();
    setTimeout(() => URL.revokeObjectURL(url),1000);
  });
  find('#import').addEventListener('click', () => find('#backup-file').click());
  find('#backup-file').addEventListener('change', async event => {
    const file = event.target.files[0]; if (!file) return;
    try {
      if (file.size > 20*1024*1024) throw Error('20MB 이하 파일을 선택해 주세요.');
      const data = JSON.parse(await file.text());
      if (data.version !== 1) throw Error('지원하지 않는 백업 버전입니다.');
      const restored = validate(data.entries);
      if (!window.confirm('백업 내용으로 모든 날짜의 기록을 교체할까요? 현재 기록을 먼저 백업하는 것을 권장합니다.')) return;
      // Persist before replacing the current in-memory records.
      localStorage.setItem(key,JSON.stringify({version:1,entries:restored}));
      entries = restored; writable = true; readable = true; saved = true; render(); status.textContent = '백업을 복원했습니다.';
    } catch (error) {status.textContent = '복원 실패: '+error.message;}
    finally {event.target.value = '';}
  });
  window.addEventListener('beforeunload',event => {if (!saved) {event.preventDefault(); event.returnValue = '';}});
  // Another tab must reload before editing to avoid silently overwriting its changes.
  window.addEventListener('storage', event => {
    if (event.key !== key && event.key !== null) return;
    writable = false; render();
    all('textarea, .md-check input, .md-actions button, [data-add]').forEach(el => {el.disabled = true;});
    status.textContent = '다른 탭에서 기록이 변경되었습니다. 현재 내용이 필요하면 백업 후 새로고침해 주세요.';
  });
  render();
})();
