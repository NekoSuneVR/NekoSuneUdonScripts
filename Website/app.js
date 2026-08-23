const LISTING_URL = "{{ listingInfo.Url }}";

function addToVcc() {
  window.location.assign(`vcc://vpm/addRepo?url=${encodeURIComponent(LISTING_URL)}`);
}

document.getElementById('add-vcc')?.addEventListener('click', addToVcc);
document.querySelectorAll('.add-package-repo').forEach((button) => {
  button.addEventListener('click', addToVcc);
});

document.getElementById('copy-repo')?.addEventListener('click', async () => {
  const field = document.getElementById('repo-url');
  if (!field) return;
  field.select();
  await navigator.clipboard.writeText(field.value);
});
