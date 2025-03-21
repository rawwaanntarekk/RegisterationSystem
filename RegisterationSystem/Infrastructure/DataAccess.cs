﻿using RegisterationSystem.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace RegisterationSystem
{
    public class DataAccess
    {
        private readonly string _connectionString;
        public DataAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);

        public IQueryable<Student> GetStudents()
        {
            using var connection = CreateConnection();
            return connection.Query<Student>("SELECT * FROM Students").AsQueryable();
        }

        public bool AddStudent(Student model)
        {
            using var connection = CreateConnection();

            return connection.Execute("INSERT INTO Students (Id, Name, Email, NormalizedEmail, Gender, Level, PasswordHash, PhotoPath) " +
                                           "Values(@Id, @Name, @Email, @NormalizedEmail, @Gender, @Level, @PasswordHash, @PhotoPath);", model) > 0;

        }

        public Student GetStudentByEmail(string email)
        {
            using var connection = CreateConnection();
            return connection.QueryFirstOrDefault<Student>("SELECT * FROM Students WHERE Email = @Email", new { Email = email })!;
        }

        public Student GetStudentById(string id)
        {
            using var connection = CreateConnection();
            return connection.QueryFirstOrDefault<Student>("SELECT * FROM Students WHERE Id = @Id", new { Id = id })!;
        }

        public bool UpdateStudent(Student model)
        {
            using var connection = CreateConnection();
            return connection.Execute("UPDATE Students SET Name = @Name, Gender = @Gender, Level = @Level, PhotoPath = @PhotoPath WHERE Id = @Id", model) > 0;
        }





        }
}
