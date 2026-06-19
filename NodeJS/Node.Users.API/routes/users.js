const express = require("express");
const router = express.Router();

const { getConnection } = require("../db");

router.get("/:id/issuedbooks", async (req, res) => {
    try {
        const pool = await getConnection();

        const result = await pool.request()
            .input("userId", req.params.id)
            .query(`
                SELECT
                    b.Id,
                    b.Title,
                    b.Author,
                    ir.IssueDate
                FROM IssueRecords ir
                INNER JOIN Books b
                    ON ir.BookId = b.Id
                WHERE ir.UserId = @userId
                  AND ir.ReturnDate IS NULL
                ORDER BY ir.IssueDate DESC
            `);

        res.json(result.recordset);
    }
    catch (err) {
        console.error(err);

        res.status(500).json({
            message: "Database error"
        });
    }
});

module.exports = router;