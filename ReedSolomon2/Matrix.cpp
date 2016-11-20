#include "stdafx.h"
#include "Matrix.h"
#include "GF16.h"
#include <iostream>

namespace ReedSolomon {

	Matrix::Matrix(int rows, int columns) : rows(rows), columns(columns) {
		matrix = new Vector*[rows];
		for (int i = 0; i < rows; i++) matrix[i] = new Vector(columns);
	}

	Matrix::Matrix(const Matrix& m) : rows(m.rows), columns(m.columns) {
		matrix = new Vector*[m.rows];
		for (int i = 0; i < rows; i++) matrix[i] = new Vector(m[i]);
	}

	Matrix::~Matrix() {
		for (int i = 0; i < rows; i++) delete matrix[i];
		delete[] matrix;
	}

	Matrix& Matrix::operator=(const Matrix& m) {
		for (int i = 0; i < rows; i++) delete matrix[i];
		if (columns != m.columns || rows != m.rows) {
			delete[] matrix;
			this->columns = m.columns;
			this->rows = m.rows;
			matrix = new Vector*[m.rows];
		}
		for (int i = 0; i < rows; i++) matrix[i] = new Vector(m[i]);
		return *this;
	}

	void Matrix::Print() const {
		for (int r = 0; r < rows; r++) {
			for (int c = 0; c < columns; c++) {
				std::cout << (*this)[r][c] << " ";
			}
			std::cout << std::endl;
		}
	}

	Vector operator*(const Matrix& m, const Vector& v) {
		Vector rv(m.GetRows());
		for (int i = 0; i < m.GetRows(); i++) rv[i] = InnerProduct(m[i], v);
		return rv;
	}

	Vector operator*(const Vector& v, const Matrix& m) {
		Vector rv(m.GetColumns());
		for (int column = 0; column < m.GetColumns(); column++) {
			for (int row = 0; row < m.GetRows(); row++) {
				rv[column] = GF16::Add(rv[column], GF16::Multiply(v[row], m[row][column]));
			}
		}
		return rv;
	}

	Matrix operator*(const Matrix& a, const Matrix& b) {
		Matrix rv(a.GetRows(), b.GetColumns());
		for (int row = 0; row < a.GetRows(); row++) {
			for (int col = 0; col < b.GetColumns(); col++) {
				for (int i = 0; i < a.GetColumns(); i++) rv[row][col] = GF16::Add(rv[row][col], GF16::Multiply(a[row][i], b[i][col]));
			}
		}
		return rv;
	}
}