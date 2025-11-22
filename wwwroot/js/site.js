// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Site-wide JavaScript
// Bootstrap's collapse is used for the navbar; no custom nav script required.

// Convert server timestamps (UTC) to the user's local 12-hour display.
document.addEventListener('DOMContentLoaded', function () {
	try {
	var nodes = document.querySelectorAll('.js-created-at');
	console.log('Local timestamp formatter executed for ' + nodes.length + ' elements');
		if (!nodes) return;
		var opts = { year: 'numeric', month: '2-digit', day: '2-digit', hour: 'numeric', minute: '2-digit', hour12: true };
		nodes.forEach(function (el) {
			var iso = el.getAttribute('data-utc');
			if (!iso) return;
			var d = new Date(iso);
			if (isNaN(d)) return;
			var formatted = new Intl.DateTimeFormat(undefined, opts).format(d);
			el.textContent = 'Requested: ' + formatted;
		});
	} catch (e) {
		// non-fatal; leave server-rendered times as-is
		console && console.warn && console.warn('local time conversion failed', e);
	}
});
