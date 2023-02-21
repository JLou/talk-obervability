-- CreateTable
CREATE TABLE IF NOT EXISTS friends (
    "id" TEXT NOT NULL,
    "name" TEXT NOT NULL,
    PRIMARY KEY ("id")
);
-- Seed
INSERT INTO friends (id, name)
VALUES ('jme', 'John 🍎');
INSERT INTO friends (id, name)
VALUES ('fde', 'François 🐦');
INSERT INTO friends (id, name)
VALUES ('gxx', 'Guillaume 👨‍👨‍👦‍👦');
INSERT INTO friends (id, name)
VALUES ('osa', 'Olivier 🔐');