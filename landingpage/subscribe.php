<?php
/**
 * Payzen HR — Waitlist subscribe endpoint
 * Accepts POST { action: 'subscribe', email, source }
 * Returns JSON { success: bool, message?: string }
 */

header('Content-Type: application/json; charset=utf-8');

if ($_SERVER['REQUEST_METHOD'] !== 'POST' || ($_POST['action'] ?? '') !== 'subscribe') {
    http_response_code(405);
    echo json_encode(['success' => false, 'message' => 'Méthode non autorisée.']);
    exit;
}

$email = filter_var(trim($_POST['email'] ?? ''), FILTER_VALIDATE_EMAIL);
if (!$email) {
    echo json_encode(['success' => false, 'message' => 'Email invalide.']);
    exit;
}

require_once __DIR__ . '/db_config.php';

try {
    // Sur hébergement mutualisé : connexion directe à la base (créée via cPanel)
    $dsn = sprintf(
        'mysql:host=%s;port=%s;dbname=%s;charset=%s',
        DB_HOST, DB_PORT, DB_NAME, DB_CHARSET
    );
    $pdo = new PDO($dsn, DB_USER, DB_PASS, [
        PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
    ]);

    // Créer la table si elle n'existe pas
    $pdo->exec("
        CREATE TABLE IF NOT EXISTS waitlist (
            id         INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
            email      VARCHAR(255) NOT NULL,
            source     VARCHAR(50)  NOT NULL DEFAULT 'hero',
            ip         VARCHAR(45)  DEFAULT NULL,
            created_at TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
            UNIQUE KEY uq_email (email)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
    ");

    $ip     = $_SERVER['HTTP_X_FORWARDED_FOR'] ?? $_SERVER['REMOTE_ADDR'] ?? null;
    $source = in_array($_POST['source'] ?? '', ['hero', 'cta']) ? $_POST['source'] : 'hero';

    $stmt = $pdo->prepare(
        "INSERT IGNORE INTO waitlist (email, source, ip) VALUES (:email, :source, :ip)"
    );
    $stmt->execute([':email' => $email, ':source' => $source, ':ip' => $ip]);

    echo json_encode(['success' => true]);

} catch (PDOException $e) {
    error_log('[Payzen Waitlist] DB Error: ' . $e->getMessage());
    // DEBUG — remove this line once the issue is identified
    echo json_encode(['success' => false, 'message' => 'DB Error: ' . $e->getMessage()]);
}
