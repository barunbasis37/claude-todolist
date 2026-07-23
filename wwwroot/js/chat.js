(function () {
    var form = document.getElementById('chatForm');
    var input = document.getElementById('chatInput');
    var log = document.getElementById('chatLog');
    var tokenInput = document.getElementById('requestToken');
    if (!form || !input || !log || !tokenInput) {
        return;
    }

    var history = [];

    function appendMessage(role, text) {
        var el = document.createElement('div');
        el.className = 'chat-message chat-message-' + role;
        el.textContent = text;
        log.appendChild(el);
        log.scrollTop = log.scrollHeight;
        return el;
    }

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        var text = input.value.trim();
        if (!text) {
            return;
        }

        appendMessage('user', text);
        history.push({ role: 'user', content: text });
        input.value = '';
        input.disabled = true;

        var pending = appendMessage('assistant', 'Thinking…');
        pending.classList.add('chat-message-pending');

        fetch(window.location.pathname + '?handler=Send', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': tokenInput.value
            },
            body: JSON.stringify({ messages: history })
        })
            .then(function (response) {
                if (!response.ok) {
                    return response.json().catch(function () { return {}; }).then(function (body) {
                        throw new Error(body.error || 'Request failed');
                    });
                }
                return response.json();
            })
            .then(function (data) {
                pending.remove();
                appendMessage('assistant', data.reply);
                history.push({ role: 'assistant', content: data.reply });
            })
            .catch(function (err) {
                pending.remove();
                appendMessage('assistant', 'Sorry, something went wrong: ' + err.message);
            })
            .finally(function () {
                input.disabled = false;
                input.focus();
            });
    });
})();
