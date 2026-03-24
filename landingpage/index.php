<?php
// Landing page has been split into separate files:
//   index.html  — markup
//   styles.css  — styles
//   main.js     — scripts
//   subscribe.php — waitlist API
header('Location: index.html', true, 301);
exit;
