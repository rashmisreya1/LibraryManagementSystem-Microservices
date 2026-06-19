const express = require("express");
const cors = require("cors");
require("dotenv").config();

const usersRoutes = require("./routes/users");

const app = express();

app.use(cors());
app.use(express.json());

app.use("/api/users", usersRoutes);

const PORT = process.env.PORT || 5150;

app.listen(PORT, () => {
    console.log(`Users API running on port ${PORT}`);
});