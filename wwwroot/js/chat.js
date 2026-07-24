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

    function escapeHtml(text) {
        return text
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function inlineFormat(text) {
        return text
            .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
            .replace(/`([^`]+?)`/g, '<code>$1</code>')
            .replace(/(^|[^*])\*([^*]+?)\*(?!\*)/g, '$1<em>$2</em>');
    }

    // Minimal, safe markdown-lite renderer: HTML-escape first, then apply a
    // small set of transforms (bold/italic/code/bullet lists) to the escaped
    // text only, so nothing the model or a todo title contains can inject
    // real markup.
    function renderRichText(text) {
        var lines = escapeHtml(text).split('\n');
        var html = '';
        var inList = false;

        lines.forEach(function (line) {
            var listMatch = line.match(/^\s*[-*]\s+(.*)/);
            if (listMatch) {
                if (!inList) {
                    html += '<ul>';
                    inList = true;
                }
                html += '<li>' + inlineFormat(listMatch[1]) + '</li>';
            } else {
                if (inList) {
                    html += '</ul>';
                    inList = false;
                }
                if (line.trim().length > 0) {
                    html += '<p>' + inlineFormat(line) + '</p>';
                }
            }
        });

        if (inList) {
            html += '</ul>';
        }

        return html || '<p></p>';
    }

    // Streams Server-Sent Events from the chat handler over a plain fetch()
    // response body, since EventSource only supports GET with no custom
    // headers/body (we need POST + the antiforgery header + a JSON payload).
    function streamChat(payload, onDelta, onError, onDone) {
        return fetch(window.location.pathname + '?handler=Send', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': tokenInput.value
            },
            body: JSON.stringify(payload)
        }).then(function (response) {
            if (!response.ok || !response.body) {
                throw new Error('Request failed (' + response.status + ')');
            }

            var reader = response.body.getReader();
            var decoder = new TextDecoder();
            var buffer = '';

            function handleEvent(raw) {
                if (!raw.trim()) {
                    return;
                }

                var eventName = 'message';
                var dataLines = [];
                raw.split('\n').forEach(function (line) {
                    if (line.indexOf('event:') === 0) {
                        eventName = line.slice(6).trim();
                    } else if (line.indexOf('data:') === 0) {
                        dataLines.push(line.slice(5).trim());
                    }
                });

                var parsed = {};
                try {
                    parsed = JSON.parse(dataLines.join('\n'));
                } catch (e) {
                    parsed = { text: dataLines.join('\n') };
                }

                if (eventName === 'delta') {
                    onDelta(parsed.text || '');
                } else if (eventName === 'error') {
                    onError(parsed.text || 'Something went wrong');
                } else if (eventName === 'done') {
                    onDone();
                }
            }

            function pump() {
                return reader.read().then(function (result) {
                    if (result.done) {
                        return;
                    }

                    buffer += decoder.decode(result.value, { stream: true });
                    var events = buffer.split('\n\n');
                    buffer = events.pop();
                    events.forEach(handleEvent);

                    return pump();
                });
            }

            return pump();
        });
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

        var assistantEl = appendMessage('assistant', '');
        assistantEl.classList.add('chat-message-pending');
        var assistantText = '';
        var hadError = false;

        streamChat({ messages: history }, function onDelta(chunk) {
            assistantEl.classList.remove('chat-message-pending');
            assistantText += chunk;
            assistantEl.innerHTML = renderRichText(assistantText);
            log.scrollTop = log.scrollHeight;
        }, function onError(message) {
            hadError = true;
            assistantEl.classList.remove('chat-message-pending');
            assistantEl.textContent = assistantText
                ? assistantText + '\n\n[Error: ' + message + ']'
                : 'Sorry, something went wrong: ' + message;
        }, function onDone() {
            if (!hadError && assistantText) {
                history.push({ role: 'assistant', content: assistantText });
            }
        }).catch(function (err) {
            assistantEl.classList.remove('chat-message-pending');
            assistantEl.textContent = 'Sorry, something went wrong: ' + err.message;
        }).finally(function () {
            input.disabled = false;
            input.focus();
        });
    });
})();
