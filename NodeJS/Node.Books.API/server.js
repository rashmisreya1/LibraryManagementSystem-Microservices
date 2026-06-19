const express = require("express");
const cors = require("cors");
require("dotenv").config();

const booksRoutes = require("./routes/books");

const app = express();

app.use(cors());
app.use(express.json());

app.use("/api/books", booksRoutes);

const PORT = process.env.PORT || 5230;

app.listen(PORT, () => {
    console.log(`Books API running on port ${PORT}`);
});