const express = require("express");
const router = express.Router();

const { sql, getConnection } = require("../db");

// Test API
router.get("/test", async (req, res) => {

    try {

        const pool = await getConnection();

        const result = await pool.request()
            .query("SELECT COUNT(*) AS TotalUsers FROM Users");

        res.json(result.recordset);

    }
    catch (err) {

        console.error(err);

        res.status(500).json({
            error: err.message
        });
    }
});

// Signup API
router.post("/signup", async (req, res) => {

    try {

        const { name, email, password } = req.body;

        const pool = await getConnection();

        await pool.request()
            .input("Name", sql.NVarChar, name)
            .input("Email", sql.NVarChar, email)
            .input("Password", sql.NVarChar, password)
            .query(`
                INSERT INTO Users (Name, Email, Password)
                VALUES (@Name, @Email, @Password)
            `);

        res.json({
            message: "Signup Successful"
        });

    }
    catch (err) {

        console.error(err);

        res.status(500).json({
            error: err.message
        });
    }
});

// Login API
router.post("/login", async (req, res) => {

    try {

        const { email, password } = req.body;

        const pool = await getConnection();

        const result = await pool.request()
            .input("Email", sql.NVarChar, email)
            .input("Password", sql.NVarChar, password)
            .query(`
                SELECT *
                FROM Users
                WHERE Email = @Email
                AND Password = @Password
            `);

        if (result.recordset.length === 0) {

            return res.status(401).json({
                message: "Invalid Credentials"
            });
        }

        res.json({
            message: "Login Successful"
        });

    }
    catch (err) {

        console.error(err);

        res.status(500).json({
            error: err.message
        });
    }
});

module.exports = router;