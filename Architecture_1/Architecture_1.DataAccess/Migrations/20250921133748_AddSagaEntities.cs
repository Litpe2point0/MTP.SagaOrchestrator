using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Architecture_1.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSagaEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SagaInstances",
                columns: table => new
                {
                    SagaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlowName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentStepName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InitialData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FlowStatus = table.Column<int>(type: "int", nullable: false),
                    ErrorStepName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaInstances", x => x.SagaId);
                });

            migrationBuilder.CreateTable(
                name: "SagaStepExcecutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SagaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TopicName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StepStatus = table.Column<int>(type: "int", nullable: false),
                    RequestData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    responseData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaStepExcecutions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SagaStepExcecutions_SagaId",
                table: "SagaStepExcecutions",
                column: "SagaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SagaInstances");

            migrationBuilder.DropTable(
                name: "SagaStepExcecutions");
        }
    }
}
