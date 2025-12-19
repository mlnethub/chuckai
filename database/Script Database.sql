-- 创建数据库 (在 PostgreSQL 中，通常需要在连接后执行，或使用 \c 命令切换)
CREATE DATABASE chucknorrisjokes
WITH
OWNER = postgres
ENCODING = 'UTF8'
LC_COLLATE = 'zh_CN.UTF-8'
LC_CTYPE = 'zh_CN.UTF-8'
TABLESPACE = pg_default
CONNECTION LIMIT = -1;
-- 注意：PostgreSQL 中没有直接的 'USE database;' 命令。
-- 您需要在连接时指定数据库，或在 psql 中使用 \c chucknorrisjokes 命令切换。

-- 创建表，调整了数据类型和默认值函数
CREATE TABLE public.chucknorrisjokes (
    Id SERIAL PRIMARY KEY, -- 使用 SERIAL 实现自增标识列
    Joke TEXT NOT NULL, -- PostgreSQL 中使用 TEXT 或 VARCHAR，TEXT 更常用且无长度限制
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, -- 使用 TIMESTAMP 和 CURRENT_TIMESTAMP
    UpdatedAt TIMESTAMP NULL
);

-- 插入数据，注意处理字符串中的单引号转义（与 SQL Server 相同）
INSERT INTO public.chucknorrisjokes (Joke) VALUES
('Chuck Norris counted to infinity. Twice.'),
('Chuck Norris can slam a revolving door.'),
('Chuck Norris doesn''t read books. He stares them down until he gets the information he wants.'),
('Chuck Norris can divide by zero.'),
('Chuck Norris can hear sign language.'),
('Chuck Norris beat the sun in a staring contest.'),
('Chuck Norris can unscramble an egg.'),
('Chuck Norris can kill two stones with one bird.'),
('Chuck Norris can do a wheelie on a unicycle.'),
('Chuck Norris makes onions cry.');
