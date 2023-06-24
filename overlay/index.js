async function getAvatar() {
  const response = await fetch('avatars.txt');
  if (!response.ok) {
    return null;
  }

  const files = (await response.text()).split('\n').filter(f => f.length > 0);
  return files[Math.floor(Math.random()*files.length)];;
}

function initSocket() {
  const socket = new WebSocket('ws://localhost:9871');

  socket.onclose   = () => { console.log('socket closed; reconnecting'); window.setTimeout(() => initSocket(), 500) }
  socket.onerror   = () => { console.log('error in socket') }
  socket.onopen    = () => { console.log('connected to socket') }
  socket.onmessage = event => { 
    const message = JSON.parse(event.data);
    switch (message.type) {
      case 'chatter':
		let chatter = message.chatter;
		if (chatter.length === 0)
		{
		  chatter = "No chatter";
		  document.getElementById('chatter-avatar').src = "avatars/Dead.png";
		}
		else
		{
		  getAvatar().then(avatar => {
		    if (avatar !== null) {
			  document.getElementById('chatter-avatar').src = avatar;
		    }
		  });
		}
		document.getElementById('chatter-name').innerText = chatter;
        break;
      case 'message':
		let m = message.message;
		if (m.length === 0)
		{
		  m = "No message";
		}
        document.getElementById('chatter-message').innerText = m;
	document.getElementById('chatter-name').style.color = message.color;
        break;
    }
  }
}

initSocket();