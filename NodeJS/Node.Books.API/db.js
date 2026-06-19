const sql = require("mssql");
require("dotenv").config();

const config = {
    server: process.env.DB_SERVER,
    port: 64936,

    user: process.env.DB_USER,
    password: process.env.DB_PASSWORD,

    database: process.env.DB_DATABASE,

    options: {
        trustServerCertificate: true
    }
};

async function getConnection() {
    return await sql.connect(config);
}

module.exports = {
    sql,
    getConnection
};