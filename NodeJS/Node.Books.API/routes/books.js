const express = require("express");
const router = express.Router();

const { getConnection } = require("../db");

router.get("/", async (req, res) => {
    try {
        const pool = await getConnection();

        const result = await pool.request().query(`
            SELECT Id, Title, Author, AvailableCopies
            FROM Books
        `);

        res.json(result.recordset);
    } catch (err) {
        console.error(err);
        res.status(500).json({
            message: "Database error"
        });
    }
});

router.get("/search", async (req, res) => {
    try {
        const title = req.query.title || "";

        const pool = await getConnection();

        const result = await pool.request()
            .input("title", `%${title}%`)
            .query(`
                SELECT Id, Title, Author, AvailableCopies
                FROM Books
                WHERE Title LIKE @title
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


router.get("/stats", async (req, res) => {
    try {
        const pool = await getConnection();

        const result = await pool.request().query(`
            SELECT
                COUNT(*) AS TotalBooks,
                SUM(AvailableCopies) AS TotalCopies
            FROM Books
        `);

        res.json(result.recordset[0]);
    }
    catch (err) {
        console.error(err);

        res.status(500).json({
            message: "Database error"
        });
    }
});

router.get("/history", async (req, res) => {
    try {
        const pool = await getConnection();

        const result = await pool.request().query(`
            SELECT
                ir.Id,
                b.Title,
                ir.UserId,
                ir.IssueDate,
                ir.ReturnDate
            FROM IssueRecords ir
            INNER JOIN Books b
                ON ir.BookId = b.Id
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

router.get("/activeborrowings", async (req, res) => {
    try {
        const pool = await getConnection();

        const result = await pool.request().query(`
            SELECT
                ir.Id,
                b.Title,
                ir.UserId,
                ir.IssueDate
            FROM IssueRecords ir
            INNER JOIN Books b
                ON ir.BookId = b.Id
            WHERE ir.ReturnDate IS NULL
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

router.get("/topissued", async (req, res) => {
    try {
        const pool = await getConnection();

        const result = await pool.request().query(`
            SELECT
                b.Id,
                b.Title,
                COUNT(ir.Id) AS IssueCount
            FROM Books b
            INNER JOIN IssueRecords ir
                ON b.Id = ir.BookId
            GROUP BY
                b.Id,
                b.Title
            ORDER BY
                IssueCount DESC
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



router.get("/:id", async (req, res) => {
    try {
        const pool = await getConnection();

        const result = await pool.request()
            .input("id", req.params.id)
            .query(`
                SELECT Id, Title, Author, AvailableCopies
                FROM Books
                WHERE Id = @id
            `);

        if (result.recordset.length === 0) {
            return res.status(404).json({
                message: "Book not found"
            });
        }

        res.json(result.recordset[0]);
    }
    catch (err) {
        console.error(err);
        res.status(500).json({
            message: "Database error"
        });
    }
});

router.post("/", async (req, res) => {
    try {
        console.log("POST request received");
        console.log(req.body);

        const { title, author, availableCopies } = req.body;

        const pool = await getConnection();

        await pool.request()
            .input("title", title)
            .input("author", author)
            .input("availableCopies", availableCopies)
            .query(`
                INSERT INTO Books (Title, Author, AvailableCopies)
                VALUES (@title, @author, @availableCopies)
            `);

        res.status(201).json({
            message: "Book added successfully"
        });
    }
    catch (err) {
        console.error(err);
        res.status(500).json({
            message: "Database error"
        });
    }
});

router.post("/issue/:id", async (req, res) => {
    try {
        const pool = await getConnection();

        const bookId = req.params.id;

        const bookResult = await pool.request()
            .input("bookId", bookId)
            .query(`
                SELECT AvailableCopies
                FROM Books
                WHERE Id = @bookId
            `);

        if (bookResult.recordset[0].AvailableCopies <= 0)
        {
            return res.status(400).json({
                message: "No copies available"
            });
        }

        await pool.request()
            .input("bookId", bookId)
            .query(`
                UPDATE Books
                SET AvailableCopies = AvailableCopies - 1
                WHERE Id = @bookId
            `);

        await pool.request()
            .input("bookId", bookId)
            .input("userId", 1)
            .query(`
                INSERT INTO IssueRecords
                (
                    BookId,
                    UserId,
                    IssueDate
                )
                VALUES
                (
                    @bookId,
                    @userId,
                    GETDATE()
                )
            `);

        res.json({
            message: "Book issued successfully"
        });
    }
    catch (err)
    {
        console.error(err);

        res.status(500).json({
            message: "Database error"
        });
    }
});




router.put("/:id", async (req, res) => {
    try {
        const { title, author, availableCopies } = req.body;

        const pool = await getConnection();

        await pool.request()
            .input("id", req.params.id)
            .input("title", title)
            .input("author", author)
            .input("availableCopies", availableCopies)
            .query(`
                UPDATE Books
                SET Title = @title,
                    Author = @author,
                    AvailableCopies = @availableCopies
                WHERE Id = @id
            `);

        res.json({
            message: "Book updated successfully"
        });
    }
    catch (err) {
        console.error(err);
        res.status(500).json({
            message: "Database error"
        });
    }
});


router.delete("/:id", async (req, res) => {
    try {
        const pool = await getConnection();

        await pool.request()
            .input("id", req.params.id)
            .query(`
                DELETE FROM Books
                WHERE Id = @id
            `);

        res.json({
            message: "Book deleted successfully"
        });
    }
    catch (err) {
        console.error(err);
        res.status(500).json({
            message: "Database error"
        });
    }
});

router.post("/return/:id", async (req, res) => {
    try {
        const pool = await getConnection();

        const bookId = req.params.id;

        const issueResult = await pool.request()
            .input("bookId", bookId)
            .query(`
                SELECT TOP 1 Id
                FROM IssueRecords
                WHERE BookId = @bookId
                  AND ReturnDate IS NULL
                ORDER BY Id DESC
            `);

        if (issueResult.recordset.length === 0)
        {
            return res.status(400).json({
                message: "Book is not currently issued"
            });
        }

        const issueId = issueResult.recordset[0].Id;

        await pool.request()
            .input("issueId", issueId)
            .query(`
                UPDATE IssueRecords
                SET ReturnDate = GETDATE()
                WHERE Id = @issueId
            `);

        await pool.request()
            .input("bookId", bookId)
            .query(`
                UPDATE Books
                SET AvailableCopies = AvailableCopies + 1
                WHERE Id = @bookId
            `);

        res.json({
            message: "Book returned successfully"
        });
    }
    catch (err)
    {
        console.error(err);

        res.status(500).json({
            message: "Database error"
        });
    }
});



module.exports = router;